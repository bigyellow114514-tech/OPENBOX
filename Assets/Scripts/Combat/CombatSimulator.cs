using UnityEngine;

public static class CombatSimulator
{
    const int MaxComboHits = 5;
    const int DefaultPlayerPetId = 102;

    public static CombatResult Run(RoleAttr playerAttr, StageData stage)
    {
        var player = new CombatantState("Player", playerAttr);
        var enemy = new CombatantState("Enemy", stage.EnemyAttr);
        var playerPet = new PetRuntimeState(PetData.Get(DefaultPlayerPetId));
        var enemyPet = new PetRuntimeState(PetData.Get(stage.EnemyPetId));
        var result = new CombatResult();
        result.PlayerMaxHp = player.MaxHp;
        result.EnemyMaxHp = enemy.MaxHp;
        result.PlayerPetId = playerPet.Pet != null ? playerPet.Pet.PetId : 0;
        result.PlayerPetResource = playerPet.Pet != null ? playerPet.Pet.PetResource : "";
        result.EnemyPetId = enemyPet.Pet != null ? enemyPet.Pet.PetId : 0;
        result.EnemyPetResource = enemyPet.Pet != null ? enemyPet.Pet.PetResource : "";

        bool playerFirst = player.Attr.Agility >= enemy.Attr.Agility;

        for (int round = 1; round <= stage.MaxRound; round++)
        {
            result.LastRound = round;

            CombatantState first = playerFirst ? player : enemy;
            CombatantState second = playerFirst ? enemy : player;
            PetRuntimeState firstPet = playerFirst ? playerPet : enemyPet;
            PetRuntimeState secondPet = playerFirst ? enemyPet : playerPet;

            ExpirePetBuff(firstPet, first);
            TakeAction(result, round, first, second, player, enemy);
            if (player.IsDead || enemy.IsDead) break;

            TryPetAction(result, round, first, second, firstPet, player, enemy);
            if (player.IsDead || enemy.IsDead) break;

            ExpirePetBuff(secondPet, second);
            TakeAction(result, round, second, first, player, enemy);
            if (player.IsDead || enemy.IsDead) break;

            TryPetAction(result, round, second, first, secondPet, player, enemy);
            if (player.IsDead || enemy.IsDead) break;
        }

        result.PlayerWon = enemy.IsDead && !player.IsDead;
        result.TimedOut = !player.IsDead && !enemy.IsDead;
        result.PlayerHp = Mathf.Max(0f, player.CurrentHp);
        result.EnemyHp = Mathf.Max(0f, enemy.CurrentHp);

        return result;
    }

    static void TryPetAction(
        CombatResult result,
        int round,
        CombatantState owner,
        CombatantState target,
        PetRuntimeState petState,
        CombatantState player,
        CombatantState enemy)
    {
        if (petState == null || petState.Pet == null || owner.IsDead || target.IsDead) return;
        if (round % Mathf.Max(1, petState.Pet.CastInterval) != 0) return;

        PetData pet = petState.Pet;
        float damage = 0f;
        float heal = 0f;
        float buffValue = 0f;

        if (pet.DamagePercent > 0f)
        {
            damage = CalculatePetDamage(owner.Attr, target.Attr, pet.DamagePercent);
            target.CurrentHp = Mathf.Max(0f, target.CurrentHp - damage);
        }

        if (pet.HealPercent > 0f)
        {
            float baseHeal = owner.Attr.Attack * Percent(pet.HealPercent);
            heal = CalculateHealing(owner.Attr, target.Attr, baseHeal);
            if (heal > 0f)
                owner.CurrentHp = Mathf.Min(owner.MaxHp, owner.CurrentHp + heal);
        }

        if (pet.HasBuff && !string.IsNullOrWhiteSpace(pet.AttrName) && pet.AttrValue != 0f)
            buffValue = ApplyPetBuff(petState, owner, pet);

        AddPetSkill(result, round, owner, target, player, enemy, pet, damage, heal, buffValue);
    }

    static float CalculatePetDamage(RoleAttr owner, RoleAttr target, float damagePercent)
    {
        float petScale = Mathf.Max(0f, 1f + Percent(owner.PetIncrease - target.PetDecrease));
        float damage = owner.Attack * Percent(damagePercent) * petScale;
        return Mathf.Ceil(Mathf.Max(1f, damage));
    }

    static float ApplyPetBuff(PetRuntimeState petState, CombatantState owner, PetData pet)
    {
        int maxStack = pet.MaxStack > 0 ? pet.MaxStack : 1;
        int nextStacks = Mathf.Min(maxStack, petState.BuffStacks + 1);
        bool gainedStack = nextStacks > petState.BuffStacks;
        petState.BuffStacks = nextStacks;
        petState.BuffRemainingOwnerTurns = Mathf.Max(0, pet.DurationRounds);

        if (!gainedStack) return 0f;

        RoleAttr delta = default;
        delta.SetByKey(pet.AttrName, pet.AttrValue);
        owner.Attr += delta;
        petState.AppliedBuff += delta;
        return pet.AttrValue;
    }

    static void ExpirePetBuff(PetRuntimeState petState, CombatantState owner)
    {
        if (petState == null || petState.BuffStacks <= 0 || petState.BuffRemainingOwnerTurns <= 0) return;

        petState.BuffRemainingOwnerTurns--;
        if (petState.BuffRemainingOwnerTurns > 0) return;

        owner.Attr -= petState.AppliedBuff;
        petState.AppliedBuff = default;
        petState.BuffStacks = 0;
    }

    static void TakeAction(
        CombatResult result,
        int round,
        CombatantState actor,
        CombatantState target,
        CombatantState player,
        CombatantState enemy)
    {
        if (actor.IsDead || target.IsDead) return;

        if (actor.Stunned)
        {
            actor.Stunned = false;
            AddPopup(result, round, actor, actor, player, enemy, "Stunned");
            return;
        }

        int comboCount = 0;
        bool keepAttacking;

        do
        {
            keepAttacking = false;
            bool interruptedByCounter = ResolveHit(result, round, actor, target, player, enemy, false);
            if (actor.IsDead || target.IsDead || interruptedByCounter) return;

            if (Roll(EffectiveRate(actor.Attr.ComboRate, target.Attr.AntiComboRate)) && comboCount < MaxComboHits)
            {
                comboCount++;
                keepAttacking = true;
                AddPopup(result, round, actor, target, player, enemy, "连击", combo: true);
            }
        }
        while (keepAttacking);
    }

    static bool ResolveHit(
        CombatResult result,
        int round,
        CombatantState actor,
        CombatantState target,
        CombatantState player,
        CombatantState enemy,
        bool isCounter)
    {
        if (Roll(EffectiveRate(target.Attr.DodgeRate, actor.Attr.AntiDodgeRate)))
        {
            AddAttack(result, round, actor, target, player, enemy, 0f, 0f, false, true, false, isCounter);
            return TryCounter(result, round, actor, target, player, enemy);
        }

        float damage = CalculateDamage(actor.Attr, target.Attr, out bool crit);
        target.CurrentHp = Mathf.Max(0f, target.CurrentHp - damage);

        float heal = CalculateLifeStealHeal(actor.Attr, target.Attr, damage);
        if (heal > 0f)
            actor.CurrentHp = Mathf.Min(actor.MaxHp, actor.CurrentHp + heal);

        bool stunned = false;
        if (!target.IsDead && !isCounter && Roll(EffectiveRate(actor.Attr.StunRate, target.Attr.AntiStunRate)))
        {
            target.Stunned = true;
            stunned = true;
        }

        AddAttack(result, round, actor, target, player, enemy, damage, heal, crit, false, stunned, isCounter);

        if (target.IsDead) return false;

        if (isCounter) return false;
        return TryCounter(result, round, actor, target, player, enemy);
    }

    static float CalculateDamage(RoleAttr actor, RoleAttr target, out bool crit)
    {
        float damage = Mathf.Max(1f, actor.Attack - target.Defence);
        float finalDamageScale = Mathf.Max(0f, 1f + Percent(actor.DamageIncrease - target.DamageDecrease));
        damage *= finalDamageScale;

        crit = Roll(EffectiveRate(actor.CritRate, target.AntiCritRate));
        if (crit)
            damage *= 1f + Percent(Mathf.Max(0f, actor.CritDmg - target.AntiCritDmg));

        return Mathf.Ceil(damage);
    }

    static float CalculateLifeStealHeal(RoleAttr actor, RoleAttr target, float damage)
    {
        float baseHeal = damage * Percent(EffectiveRate(actor.LifeStealRate, target.AntiLifeStealRate));
        return CalculateHealing(actor, target, baseHeal);
    }

    public static float CalculateHealing(RoleAttr healer, RoleAttr target, float baseHeal)
    {
        float healingScale = Mathf.Max(0f, 1f + Percent(healer.Healing - target.AntiHealing));
        return Mathf.Floor(Mathf.Max(0f, baseHeal) * healingScale);
    }

    static bool TryCounter(
        CombatResult result,
        int round,
        CombatantState actor,
        CombatantState target,
        CombatantState player,
        CombatantState enemy)
    {
        if (!Roll(EffectiveRate(target.Attr.CounterRate, actor.Attr.AntiCounterRate))) return false;

        AddPopup(result, round, target, actor, player, enemy, "反击");
        ResolveHit(result, round, target, actor, player, enemy, true);
        return true;
    }

    static bool Roll(float ratePercent)
    {
        return Random.value < Mathf.Clamp01(ratePercent / 100f);
    }

    static float EffectiveRate(float ratePercent, float antiRatePercent)
    {
        return Mathf.Max(0f, ratePercent - antiRatePercent);
    }

    static float Percent(float value)
    {
        return value / 100f;
    }

    static void AddAttack(
        CombatResult result,
        int round,
        CombatantState actor,
        CombatantState target,
        CombatantState player,
        CombatantState enemy,
        float damage,
        float heal,
        bool crit,
        bool dodged,
        bool stunned,
        bool counter)
    {
        var entry = NewEntry(CombatEventType.Attack, round, actor, target, player, enemy, "");
        entry.Damage = damage;
        entry.Heal = heal;
        entry.Crit = crit;
        entry.Dodged = dodged;
        entry.Stunned = stunned;
        entry.Counter = counter;
        result.Logs.Add(entry);
    }

    static void AddPopup(
        CombatResult result,
        int round,
        CombatantState actor,
        CombatantState target,
        CombatantState player,
        CombatantState enemy,
        string text,
        bool combo = false)
    {
        var entry = NewEntry(CombatEventType.Popup, round, actor, target, player, enemy, text);
        entry.Combo = combo;
        result.Logs.Add(entry);
    }

    static void AddPetSkill(
        CombatResult result,
        int round,
        CombatantState owner,
        CombatantState target,
        CombatantState player,
        CombatantState enemy,
        PetData pet,
        float damage,
        float heal,
        float buffValue)
    {
        string text = pet != null && !string.IsNullOrEmpty(pet.PetName) ? pet.PetName : "Pet";
        var entry = NewEntry(CombatEventType.PetSkill, round, owner, target, player, enemy, text);
        entry.PetId = pet != null ? pet.PetId : 0;
        entry.PetResource = pet != null ? pet.PetResource : "";
        entry.PetActorIsPlayer = owner == player;
        entry.Damage = damage;
        entry.Heal = heal;
        entry.BuffValue = buffValue;
        entry.BuffAttrName = pet != null ? pet.AttrName : "";
        result.Logs.Add(entry);
    }

    static CombatLogEntry NewEntry(
        CombatEventType type,
        int round,
        CombatantState actor,
        CombatantState target,
        CombatantState player,
        CombatantState enemy,
        string text)
    {
        return new CombatLogEntry(
            type,
            round,
            text,
            actor == player,
            target == player,
            player.CurrentHp,
            enemy.CurrentHp,
            player.Stunned,
            enemy.Stunned);
    }

    sealed class PetRuntimeState
    {
        public readonly PetData Pet;
        public int BuffStacks;
        public int BuffRemainingOwnerTurns;
        public RoleAttr AppliedBuff;

        public PetRuntimeState(PetData pet)
        {
            Pet = pet;
        }
    }
}
