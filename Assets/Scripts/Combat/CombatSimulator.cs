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

            if (Roll(actor.Attr.ComboRate) && comboCount < MaxComboHits)
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
        if (Roll(target.Attr.DodgeRate))
        {
            AddAttack(result, round, actor, target, player, enemy, 0f, 0f, false, true, false, isCounter);
            return TryCounter(result, round, actor, target, player, enemy);
        }

        float damage = Mathf.Max(1f, actor.Attr.Attack - target.Attr.Defence);
        bool crit = Roll(actor.Attr.CritRate);
        if (crit)
            damage *= 1f + Mathf.Max(0f, actor.Attr.CritDmg) / 100f;

        damage = Mathf.Ceil(damage);
        target.CurrentHp = Mathf.Max(0f, target.CurrentHp - damage);

        string line = actor.Name + " hits " + target.Name + " for " + damage.ToString("0");
        if (crit) line += " (crit)";
        if (isCounter) line += " (counter)";

        float heal = Mathf.Floor(damage * Mathf.Max(0f, actor.Attr.LifeStealRate) / 100f);
        if (heal > 0f)
            actor.CurrentHp = Mathf.Min(actor.MaxHp, actor.CurrentHp + heal);

        bool stunned = false;
        if (!target.IsDead && !isCounter && Roll(actor.Attr.StunRate))
        {
            target.Stunned = true;
            stunned = true;
        }

        AddAttack(result, round, actor, target, player, enemy, damage, heal, crit, false, stunned, isCounter);

        if (target.IsDead) return false;

        if (isCounter) return false;
        return TryCounter(result, round, actor, target, player, enemy);
    }

    static bool TryCounter(
        CombatResult result,
        int round,
        CombatantState actor,
        CombatantState target,
        CombatantState player,
        CombatantState enemy)
    {
        if (!Roll(target.Attr.CounterRate)) return false;

        AddPopup(result, round, target, actor, player, enemy, "Counter");
        ResolveHit(result, round, target, actor, player, enemy, true);
        return true;
    }

    static bool Roll(float ratePercent)
    {
        return Random.value < Mathf.Clamp01(ratePercent / 100f);
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
