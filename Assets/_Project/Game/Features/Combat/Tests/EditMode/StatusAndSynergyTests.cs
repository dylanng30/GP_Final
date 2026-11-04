using System.Collections.Generic;
using Dylanng.Gameplay.RPG.Stats;
using Game.Data.Hero;
using NUnit.Framework;
using UnityEngine;

namespace Game.Combat.Domain.Tests
{
    public class StatusAndSynergyTests
    {
        private const float Tolerance = 0.001f;

        private readonly List<Object> _createdAssets = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object asset in _createdAssets) Object.DestroyImmediate(asset);
            _createdAssets.Clear();
        }

        private T Create<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _createdAssets.Add(asset);
            return asset;
        }

        private StatusDefinitionSO CreateStatus(
            ControlFlags flags = ControlFlags.None,
            StatusStackPolicy policy = StatusStackPolicy.Refresh,
            int maxStacks = 1)
        {
            StatusDefinitionSO status = Create<StatusDefinitionSO>();
            status.ControlFlags = flags;
            status.StackPolicy = policy;
            status.MaxStacks = maxStacks;
            return status;
        }

        [Test]
        public void Status_ExpiresAfterDuration()
        {
            var set = new StatusSet();
            StatusDefinitionSO status = CreateStatus();
            set.Apply(status, 1f, 0f, null);

            set.Tick(0.5f);
            Assert.That(set.Has(status), Is.True);

            set.Tick(0.6f);
            Assert.That(set.Has(status), Is.False);
        }

        [Test]
        public void Refresh_KeepsLongestDuration()
        {
            var set = new StatusSet();
            StatusDefinitionSO status = CreateStatus();

            set.Apply(status, 5f, 0f, null);
            StatusInstance instance = set.Apply(status, 2f, 0f, null);

            Assert.That(instance.Remaining, Is.EqualTo(5f).Within(Tolerance));
            Assert.That(set.Active.Count, Is.EqualTo(1));
        }

        [Test]
        public void Stack_IsLimitedByMaxStacks()
        {
            var set = new StatusSet();
            StatusDefinitionSO status = CreateStatus(policy: StatusStackPolicy.Stack, maxStacks: 2);

            set.Apply(status, 1f, 0f, null);
            set.Apply(status, 1f, 0f, null);
            StatusInstance instance = set.Apply(status, 1f, 0f, null);

            Assert.That(instance.Stacks, Is.EqualTo(2));
        }

        [Test]
        public void KeepExisting_IgnoresNewApplication()
        {
            var set = new StatusSet();
            StatusDefinitionSO status = CreateStatus(policy: StatusStackPolicy.KeepExisting);

            set.Apply(status, 1f, 0f, null);
            StatusInstance instance = set.Apply(status, 9f, 0f, null);

            Assert.That(instance.Remaining, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void ControlFlags_StayUntilLastStatusEnds()
        {
            var set = new StatusSet();
            StatusDefinitionSO shortStun = CreateStatus(ControlFlags.CannotMove | ControlFlags.CannotAct);
            StatusDefinitionSO longRoot = CreateStatus(ControlFlags.CannotMove);
            set.Apply(shortStun, 1f, 0f, null);
            set.Apply(longRoot, 3f, 0f, null);

            set.Tick(1.5f);

            Assert.That(set.Flags, Is.EqualTo(ControlFlags.CannotMove));

            set.Tick(2f);

            Assert.That(set.Flags, Is.EqualTo(ControlFlags.None));
        }

        [Test]
        public void ControlLocks_DoNotInterfereWithEachOther()
        {
            var set = new StatusSet();
            object first = set.AddLock(ControlFlags.CannotAct);
            object second = set.AddLock(ControlFlags.CannotAct);

            set.RemoveLock(first);
            Assert.That(set.Flags, Is.EqualTo(ControlFlags.CannotAct));

            set.RemoveLock(second);
            Assert.That(set.Flags, Is.EqualTo(ControlFlags.None));
        }

        [Test]
        public void ControlChanged_RaisedOnlyWhenFlagsChange()
        {
            var set = new StatusSet();
            StatusDefinitionSO stun = CreateStatus(ControlFlags.CannotAct);
            int changedCount = 0;
            set.ControlChanged += () => changedCount++;

            set.Apply(stun, 1f, 0f, null);
            set.Apply(stun, 1f, 0f, null);

            Assert.That(changedCount, Is.EqualTo(1));
        }

        [Test]
        public void Ticks_FollowInterval()
        {
            var set = new StatusSet();
            StatusDefinitionSO burn = CreateStatus();
            burn.TickInterval = 1f;
            int tickCount = 0;
            set.Ticked += _ => tickCount++;
            set.Apply(burn, 3.5f, 10f, null);

            for (int i = 0; i < 35; i++) set.Tick(0.1f);

            Assert.That(tickCount, Is.EqualTo(3));
        }

        [Test]
        public void OutgoingMultiplier_OnlyAffectsMatchingElement()
        {
            var set = new StatusSet();
            StatusDefinitionSO burn = CreateStatus();
            burn.OutgoingDamageModifiers.Add(new ElementDamageModifier(GameElement.Metal, 0.8f));
            set.Apply(burn, 1f, 0f, null);

            Assert.That(set.GetOutgoingMultiplier(GameElement.Metal), Is.EqualTo(0.8f).Within(Tolerance));
            Assert.That(set.GetOutgoingMultiplier(GameElement.Fire), Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Reaction_MatchesIncomingAndRequiredStatus()
        {
            StatusDefinitionSO wet = CreateStatus();
            StatusDefinitionSO burn = CreateStatus();
            StatusDefinitionSO root = CreateStatus();
            ReactionRuleSO rule = Create<ReactionRuleSO>();
            rule.Incoming = wet;
            rule.RequiredOnTarget = burn;

            var set = new StatusSet();
            set.Apply(burn, 5f, 0f, null);
            var results = new List<ReactionRuleSO>();

            ReactionTable.FindMatches(new[] { rule }, wet, set, results);
            Assert.That(results, Has.Count.EqualTo(1));

            ReactionTable.FindMatches(new[] { rule }, root, set, results);
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void Synergy_RequiresBothElements()
        {
            ElementSynergySO woodToFire = Create<ElementSynergySO>();
            woodToFire.Source = GameElement.Wood;
            woodToFire.Target = GameElement.Fire;
            var results = new List<ElementSynergySO>();

            SynergyEvaluator.FindActive(new[] { GameElement.Wood, GameElement.Fire }, new[] { woodToFire }, 0, results);
            Assert.That(results, Has.Count.EqualTo(1));

            SynergyEvaluator.FindActive(new[] { GameElement.Wood, GameElement.Earth }, new[] { woodToFire }, 0, results);
            Assert.That(results, Is.Empty);
        }

        [Test]
        public void Synergy_RespectsMaxLinks()
        {
            ElementSynergySO first = Create<ElementSynergySO>();
            first.Source = GameElement.Wood;
            first.Target = GameElement.Fire;
            ElementSynergySO second = Create<ElementSynergySO>();
            second.Source = GameElement.Fire;
            second.Target = GameElement.Earth;
            var elements = new[] { GameElement.Wood, GameElement.Fire, GameElement.Earth };
            var results = new List<ElementSynergySO>();

            SynergyEvaluator.FindActive(elements, new[] { first, second }, 1, results);

            Assert.That(results, Has.Count.EqualTo(1));
        }

        [Test]
        public void StatBuff_KeepsConfiguredValues()
        {
            var buff = new StatBuff(StatType.CritRate, 0.1f, StatModType.Flat);

            Assert.That(buff.StatType, Is.EqualTo(StatType.CritRate));
            Assert.That(buff.Value, Is.EqualTo(0.1f).Within(Tolerance));
        }
    }
}
