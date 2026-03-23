using System.Collections.Generic;
using System.Runtime.CompilerServices;
using nel;
using nel.smnp;
using XX;

namespace GenshinInCradle;

public class SummonerPlayerAdvanced : SummonerPlayer {
    private static long time = 0;

    public SummonerPlayerAdvanced(EnemySummoner _Summoner, EfParticleFuncCalc _FuncBase, CsvReaderA _CR,
        out bool bgm_replaced, bool auto_prepare_content = true)
        : base(_Summoner, _FuncBase, _CR, out bgm_replaced, auto_prepare_content) {
    }

    public static void log(double id, [CallerLineNumber] int line = 0) {
        // long end = System.Diagnostics.Stopwatch.GetTimestamp();
        // Console.WriteLine($"#{id} - Line {line} - XORSP #{SeedSet.randCount} - {end - time}");
        // time = System.Diagnostics.Stopwatch.GetTimestamp();
    }

    public override void prepareEnemyConentInner(ref bool bgm_replaced) {
        log(1);
        var m2D = Lp.Mp.M2D as NelM2DBase;
        Asplitter_title.Clear();
        Asplitter_title.Add(new SplitterTerm());
        var nightCon = Lp.nM2D.NightCon;
        int thunder_overdrive;
        m2D.NightCon.SummonerInited(Summoner, Lp.Mp, out thunder_overdrive);
        var _overdrive_capacity_weather = X.Mx(0, thunder_overdrive + Summoner.get_thunder_odable_def());
        var _count_add = nightCon.summoner_enemy_count_addition(Summoner);
        var _appear_add = nightCon.summoner_max_addition(this);
        log(2);
        var CI = new SPCntInfo(_appear_add, _count_add, _overdrive_capacity_weather) {
            auto_activate_immediate_summon = Lp.auto_activate_enemy
        };
        log(2.1);
        var nattr_addable_count = nightCon.summoner_attachable_nattr_max(this);
        max_enemy_appear_whole = X.Mx(2, 3 + _appear_add);
        thunder_odable_appear = Summoner.thunder_odable_appear + nightCon.summoner_max_thunder_odable_appear(this);
        bgm_block_ovr = null;
        Summoner.GetManager();
        log(2.4);
        var targetEnemyKinds = new List<SmnEnemyKind>(4);
        var ASmnPosL = new List<SmnPoint>();
        prepareEnemyConentFromScript(targetEnemyKinds, ASmnPosL, ref CI);
        log(2.7);
        var smnEnemyKindList2 = new List<SmnEnemyKind>(targetEnemyKinds.Count);
        smnEnemyKindList2.Clear();
        smnEnemyKindList2.AddRange(targetEnemyKinds);
        var num1 = 0;
        var num2 = 0.0f;
        targetEnemyKinds.Clear();
        log(3);
        var numArrayList = new List<int[]>(2);
        for (var index1 = smnEnemyKindList2.Count - 1; index1 >= 0; --index1) {
            var Src = smnEnemyKindList2[index1];
            numArrayList.Clear();
            var num3 = 999;
            if (CI.OEnemyCountMax != null)
                foreach (var keyValuePair in CI.OEnemyCountMax)
                    if (Src.isSame(keyValuePair.Key)) {
                        numArrayList.Add(keyValuePair.Value);
                        num3 = X.Mn(keyValuePair.Value[0] - keyValuePair.Value[1], num3);
                    }

            var num4 = X.Mn(num3, Src.def_count);
            if (num3 <= Src.def_count)
                Src.count_add_weight = 0.0f;
            for (var index2 = numArrayList.Count - 1; index2 >= 0; --index2)
                numArrayList[index2][1] += num4;
            if (!Src.count_fix)
                num2 += Src.count_add_weight;
            if (Src.pre_overdrive)
                num1 += num4;
            while (--num4 >= 0)
                targetEnemyKinds.Add(new SmnEnemyKind(Src));
        }

        log(4);
        if (num2 > 0.0 && smnEnemyKindList2.Count > 0) {
            shuffle(smnEnemyKindList2);
            var num5 = 0;
            while (_count_add > 0 && num2 > 0.0 && ++num5 < 300) {
                var Src = (SmnEnemyKind)null;
                if (CI.countadd_priority <= 0) {
                    var v1 = 1;
                    for (var index = smnEnemyKindList2.Count - 1; index >= 0; --index) {
                        var smnEnemyKind = smnEnemyKindList2[index];
                        if (!smnEnemyKind.count_fix) {
                            v1 = X.Mn(v1, smnEnemyKind.def_count);
                            if (smnEnemyKind.def_count == CI.countadd_priority) {
                                Src = smnEnemyKind;
                                Src.def_count = 1;
                                break;
                            }
                        }
                    }

                    CI.countadd_priority = v1;
                }

                if (Src == null && CI.countadd_priority > 0) {
                    var num6 = XORSP() * num2;
                    for (var index = smnEnemyKindList2.Count - 1; index >= 0; --index) {
                        var smnEnemyKind = smnEnemyKindList2[index];
                        if (smnEnemyKind.count_add_weight > 0.0) {
                            num6 -= smnEnemyKind.count_add_weight;
                            if (num6 <= 0.00019999999494757503) {
                                Src = smnEnemyKind;
                                break;
                            }
                        }
                    }
                }

                if (Src != null) {
                    --_count_add;
                    var smnEnemyKind = new SmnEnemyKind(Src);
                    smnEnemyKind.pre_overdrive = false;
                    targetEnemyKinds.Add(smnEnemyKind);
                    if (CI.OEnemyCountMax != null) {
                        var flag = false;
                        foreach (var keyValuePair in CI.OEnemyCountMax)
                            if (smnEnemyKind.isSame(keyValuePair.Key) &&
                                ++keyValuePair.Value[1] >= keyValuePair.Value[0])
                                flag = true;
                        if (flag) {
                            num2 -= Src.count_add_weight;
                            Src.count_add_weight = 0.0f;
                        }
                    }
                }
            }
        }

        smnEnemyKindList2.Clear();
        log(5);
        for (var index3 = 0; index3 < 2; ++index3) {
            if (CI.odable_enemy_exist && CI.overdrive_capacity_weather > 0) {
                var count = targetEnemyKinds.Count;
                var A = X.makeCountUpArray(count);
                shuffle(A, count);
                for (var index4 = 0; index4 < count; ++index4) {
                    var index5 = A[index4];
                    var smnEnemyKind = targetEnemyKinds[index5];
                    var enemyDesc = smnEnemyKind.EnemyDesc;
                    if (!smnEnemyKind.pre_overdrive && !smnEnemyKind.thunder_overdrive && enemyDesc.overdriveable) {
                        smnEnemyKind.thunder_overdrive = true;
                        smnEnemyKindList2.Add(smnEnemyKind);
                        ++num1;
                        if (--CI.overdrive_capacity_weather <= 0)
                            break;
                    }
                }
            }

            if (index3 == 0) {
                if (targetEnemyKinds.Count > CI.max_enemy_count) {
                    for (var index6 = smnEnemyKindList2.Count - 1; index6 >= 0; --index6)
                        targetEnemyKinds.Remove(smnEnemyKindList2[index6]);
                    shuffle(targetEnemyKinds);
                    var count = targetEnemyKinds.Count - (CI.max_enemy_count + smnEnemyKindList2.Count);
                    if (count > 0)
                        targetEnemyKinds.RemoveRange(CI.max_enemy_count + smnEnemyKindList2.Count, count);
                    for (var index7 = smnEnemyKindList2.Count - 1; index7 >= 0; --index7)
                        targetEnemyKinds.Add(smnEnemyKindList2[index7]);
                }
                else {
                    shuffle(targetEnemyKinds);
                }

                if (QEntry.valid && QEntry.fix_enemykind >= 0) {
                    float mp_min;
                    float mp_max;
                    var num7 = nightCon.summoner_enemy_for_qentry_special_count_min(this, out mp_min, out mp_max);
                    var fixEnemykind = (ENEMYID)QEntry.fix_enemykind;
                    var v1 = 0;
                    for (var index8 = targetEnemyKinds.Count - 1; index8 >= 0; --index8) {
                        var K = targetEnemyKinds[index8];
                        if (!is_follower(K))
                            v1 = X.Mx(v1, K.splitter_id);
                        if (K.isSame(fixEnemykind))
                            --num7;
                    }

                    if (num7 > 0)
                        while (--num7 >= 0) {
                            var smnEnemyKind = new SmnEnemyKind(NDAT.ToStr(fixEnemykind & ~ENEMYID._OVERDRIVE_FLAG), 1,
                                CI.splitter_id, mp_min, mp_max, "", _mp_add_weight: 3f) {
                                pre_overdrive = (fixEnemykind & ENEMYID._OVERDRIVE_FLAG) > 0
                            };
                            targetEnemyKinds.Add(smnEnemyKind);
                        }
                    else
                        break;
                }
                else {
                    break;
                }
            }
            else {
                break;
            }
        }

        log(6);
        if (CI.splitter_id >= 1)
            targetEnemyKinds.Sort(fnSortKind);
        smnEnemyKindList2.Clear();
        prepareEnemyEnAttr(targetEnemyKinds, smnEnemyKindList2, nattr_addable_count);
        log(7);
        log(8);
        log(9);
        ASmnPos = ASmnPosL.ToArray();
        AKindRest = targetEnemyKinds;
        AKindRest.Sort(fnSortKind);
        log(10);
    }
}