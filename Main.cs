[assembly: MelonInfo(typeof(AllDartling.Main), "All Dartling", "1.0.0", "Myself")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace AllDartling;

public class Main : BloonsTD6Mod
{
    public static readonly ModSettingBool DartlingEnabled = new(true)
    {
        displayName = "All Dartling Enabled",
        button = true,
        enabledText = "ENABLED",
        disabledText = "DISABLED"
    };

    public static readonly ModSettingBool UseDartlingFireRate = new(true)
    {
        displayName = "Use Dartling Fire Rate",
        description = "When enabled, all towers fire at dartling gunner speed. When disabled, towers keep their original fire rate.",
        button = true,
        enabledText = "ENABLED",
        disabledText = "DISABLED"
    };

    public static readonly ModSettingHotkey ToggleKey = new(KeyCode.F8)
    {
        displayName = "Toggle All Dartling ON/OFF",
        description = "Quickly enable/disable dartling mode for all towers"
    };

    public static readonly ModSettingHotkey ToggleFireRateKey = new(KeyCode.F9)
    {
        displayName = "Toggle Dartling Fire Rate ON/OFF",
        description = "Quickly enable/disable dartling fire rate for all towers"
    };

    private static TargetPointerModel? _targetPointerTemplate;
    private static RotateToPointerModel? _rotateToPointerTemplate;
    private static float _dartlingFireRate = 0f;

    private static readonly string[] _towerExclusions =
    {
        "mortar", "spikefactory", "alchemist", "bananafarm"
    };

    private static void EnsureTemplates(GameModel? gm)
    {
        if (_targetPointerTemplate != null && _rotateToPointerTemplate != null) return;
        if (gm == null) return;

        try
        {
            var dartling = gm.GetTowerFromId("DartlingGunner");
            var dartlingAttack = dartling.GetAttackModel();

            _targetPointerTemplate = dartlingAttack.GetBehavior<TargetPointerModel>().Duplicate();
            _rotateToPointerTemplate = dartlingAttack.GetBehavior<RotateToPointerModel>().Duplicate();

            if (_dartlingFireRate == 0f && dartlingAttack.weapons.Length > 0)
            {
                _dartlingFireRate = dartlingAttack.weapons[0].rate;
            }
        }
        catch
        {
            MelonLogger.Warning("[AllDartling] Could not acquire dartling gunner templates.");
        }
    }

    private static void MakeTowerDartling(TowerModel tm, bool useDartlingFireRate)
    {
        if (tm == null) return;
        if (_targetPointerTemplate == null || _rotateToPointerTemplate == null) return;

        var lower = tm.name.ToLowerInvariant();
        if (_towerExclusions.Any(excl => lower.Contains(excl)))
            return;

        tm.isGlobalRange = true;

        var isHeli = lower.Contains("heli");
        var isBuccaneer = lower.Contains("buccaneer");
        var skipRotation = isHeli || isBuccaneer;

        var isEngineer = lower.Contains("engineer");
        var attacks = tm.GetAttackModels();

        for (int i = 0; i < attacks.Count; i++)
        {
            var attack = attacks[i];

            if (isEngineer && i > 0)
                continue;

            attack.RemoveBehavior<TargetFirstModel>();
            attack.RemoveBehavior<TargetLastModel>();
            attack.RemoveBehavior<TargetCloseModel>();
            attack.RemoveBehavior<TargetStrongModel>();
            attack.RemoveBehavior<PursuitSettingModel>();
            attack.RemoveBehavior<AttackFilterModel>();

            if (!attack.HasBehavior<TargetPointerModel>())
                attack.AddBehavior(_targetPointerTemplate.Duplicate());

            if (!skipRotation && !attack.HasBehavior<RotateToPointerModel>())
                attack.AddBehavior(_rotateToPointerTemplate.Duplicate());

            attack.fireWithoutTarget = true;
            attack.attackThroughWalls = true;
            attack.range = 1000f;

            foreach (var weapon in attack.weapons)
            {
                weapon.fireWithoutTarget = true;

                if (useDartlingFireRate && _dartlingFireRate > 0f)
                {
                    weapon.rate = _dartlingFireRate;
                }
            }
        }
    }

    public override void OnTowerCreated(Tower tower, Entity target, Model modelToUse)
    {
        if (!DartlingEnabled) return;

        var gm = InGame.instance?.GetGameModel() ?? Game.instance?.model;
        EnsureTemplates(gm);

        var modified = tower.towerModel.Duplicate();
        MakeTowerDartling(modified, UseDartlingFireRate);
        tower.UpdateRootModel(modified);
    }

    public override void OnTowerUpgraded(Tower tower, string upgradeName, TowerModel newBaseTowerModel)
    {
        if (!DartlingEnabled) return;

        var gm = InGame.instance?.GetGameModel() ?? Game.instance?.model;
        EnsureTemplates(gm);

        var modified = tower.towerModel.Duplicate();
        MakeTowerDartling(modified, UseDartlingFireRate);
        tower.UpdateRootModel(modified);
    }

    public override void OnUpdate()
    {
        if (ToggleKey.JustPressed())
        {
            DartlingEnabled.SetValueAndSave(!DartlingEnabled);
            MelonLogger.Msg($"[AllDartling] {(DartlingEnabled ? "ENABLED" : "DISABLED")}");
        }

        if (ToggleFireRateKey.JustPressed())
        {
            UseDartlingFireRate.SetValueAndSave(!UseDartlingFireRate);
            MelonLogger.Msg($"[AllDartling] Fire Rate: {(UseDartlingFireRate ? "DARTLING SPEED" : "ORIGINAL SPEED")}");
        }
    }
}
