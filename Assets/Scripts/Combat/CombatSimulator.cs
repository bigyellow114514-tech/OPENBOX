using UnityEngine;

public static class CombatSimulator
{
    const int MaxComboHits = 5;

    public static CombatResult Run(RoleAttr playerAttr, StageData stage)
    {
        var player = new CombatantState("Player", playerAttr);
        var enemy = new CombatantState("Enemy", stage.EnemyAttr);
        var result = new CombatResult();
        result.PlayerMaxHp = player.MaxHp;
        result.EnemyMaxHp = enemy.MaxHp;

        bool playerFirst = player.Attr.Agility >= enemy.Attr.Agility;

        for (int round = 1; round <= stage.MaxRound; round++)
        {
            result.LastRound = round;

            CombatantState first = playerFirst ? player : enemy;
            CombatantState second = playerFirst ? enemy : player;

            TakeAction(result, round, first, second, player, enemy);
            if (player.IsDead || enemy.IsDead) break;

            TakeAction(result, round, second, first, player, enemy);
            if (player.IsDead || enemy.IsDead) break;
        }

        result.PlayerWon = enemy.IsDead && !player.IsDead;
        result.TimedOut = !player.IsDead && !enemy.IsDead;
        result.PlayerHp = Mathf.Max(0f, player.CurrentHp);
        result.EnemyHp = Mathf.Max(0f, enemy.CurrentHp);

        return result;
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
                AddPopup(result, round, actor, target, player, enemy, "Combo", combo: true);
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
        float finalDamageScale = Mathf.Max(1f, 1f + Percent(actor.DamageIncrease - target.DamageDecrease));
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

        AddPopup(result, round, target, actor, player, enemy, "Counter");
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
}
