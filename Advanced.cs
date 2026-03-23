using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using HarmonyLib;
using m2d;
using nel;
using UnityEngine;
using XX;
using Logger = XX.Logger;

namespace GenshinInCradle;

public class Advanced {
    public static long passedFrames;
    private static long lastTicks;
    private static long nowTicks;
    public static double passedGt;
    public static double lastGt;
    public static List<M2Ray> rayList = new();

    private static bool isAdvancedEnabled() {
        return Configs.configAdvancedBool.Value;
    }

    [HarmonyPatch(typeof(M2Ray), "CastRayAndCollider")]
    [HarmonyPostfix]
    public static void cast(ref RaycastHit2D AHit, M2Ray __instance) {
        if (!isAdvancedEnabled()) return;
        rayList.Add(__instance);
        // Console.WriteLine("cast! raylist "+(rayList.Count));
    }

    public static void cast2(M2Ray instance, out float num, out float num2, out float num3, out float num4) {
        var shape = instance.shape;
        var radius = instance.radius;
        num = num4 = 0;
        num2 = num3 = 1;
        if (shape == RAYSHAPE.RECT) goto end;
        if (shape == RAYSHAPE.DAIA) {
            num4 = 45f;
        }
        else if (shape == RAYSHAPE.RECT_HH) {
            num2 = 1.33f;
        }
        else if (shape == RAYSHAPE.RECT_HH2) {
            num2 = 2f;
        }
        else if (shape == RAYSHAPE.RECT_HH2C) {
            num3 = 0.5f;
        }
        else if (shape == RAYSHAPE.RECT_VH) {
            num3 = 1.33f;
        }
        else {
            num2 = 0f;
            num3 = radius;
        }

        end: ;
    }

    private static Tuple<float, float> toScreen(ref float x, ref float y) {
        var m2d = M2DBase.Instance as NelM2DBase;
        var mp = m2d.curMap;
        x = mp.ux2effectScreenx(mp.map2ux(x)) * 64;
        y = mp.uy2effectScreeny(mp.map2uy(y)) * 64;
        return new Tuple<float, float>(x, y);
    }

    public static void renderWholeMoverMode2(ref ProjectionContainer JCon, ref Camera Cam, ref int draw_id,
        ref List<M2RenderTicket>[] ___AADob) {
        var list = ___AADob[draw_id];
        var m2d = M2DBase.Instance as NelM2DBase;
        var mp = m2d.curMap;
        GL.LoadProjectionMatrix(JCon.CameraProjectionTransformed);
        if (Configs.configDisplayAttackBox.Value)
            foreach (var ray in rayList) {
                var md2 = new MeshDrawer();
                md2.activate("attack_box_refreshed", MTRX.MtrMeshNormal, false, C32.d2c(0xFFFFFFFFU));
                md2.Col = C32.d2c(0xFFFFFFFFU);
                if (ray.Caster == null || ray.Caster.transform == null) {
                    Console.WriteLine("returned because of null matrix");
                    continue;
                }

                float num, num2, num3, num4;
                var clenb = mp.CLENB;
                cast2(ray, out num, out num2, out num3, out num4);
                float x = ray.getUPos().x / clenb * 64, y = ray.getUPos().y / clenb * 64;
                float ratio = 64;
                if (num2 == 0f) {
                    float sx = x * clenb, sy = y * clenb, r = num3 * ratio;
                    var tx = sx + ray.Dir.x * ray.len * ratio;
                    var ty = sy + ray.Dir.y * ray.len * ratio;
                    var dis = (float)Math.Sqrt((sx - tx) * (sx - tx) + (sy - ty) * (sy - ty));
                    List<Vector4> v = new();
                    const int n = 12;
                    for (var j = 0; j <= n; j++) {
                        var angle = Math.PI * j / n + Math.PI / 2;
                        v.Add(new Vector4((float)Math.Cos(angle) * r, (float)Math.Sin(angle) * r, 0, 1));
                    }

                    for (var j = 0; j <= n; j++) {
                        var angle = Math.PI * j / n - Math.PI / 2;
                        v.Add(new Vector4((float)Math.Cos(angle) * r + dis, (float)Math.Sin(angle) * r, 0, 1));
                    }

                    var m = v.Count;
                    var q = Quaternion.AngleAxis((float)(Math.Atan2(ty - sy, tx - sx) * 180.0 / Math.PI),
                        new Vector3(0, 0, 1));
                    var transform = Matrix4x4.Translate(new Vector3(sx, sy, 0)) * Matrix4x4.Rotate(q);
                    md2.Col = C32.d2c(0xBFFF0000);
                    for (var j = 2; j < m; j++) {
                        var v0 = transform * v[0];
                        var v1 = transform * v[j];
                        var v2 = transform * v[j - 1];
                        md2.Triangle(v0.x, v0.y, v1.x, v1.y, v2.x, v2.y);
                    }

                    md2.Col = C32.d2c(0xFF00FF00);
                    md2.Line(sx, sy, tx, ty, 3);
                }
                else {
                    float sx = x * clenb, sy = y * clenb;
                    var tx = sx + ray.Dir.x * ray.len * ratio;
                    var ty = sy + ray.Dir.y * ray.len * ratio;
                    var dis = (float)Math.Sqrt((sx - tx) * (sx - tx) + (sy - ty) * (sy - ty));
                    num2 *= ray.radius * ratio;
                    num3 *= ray.radius * ratio;

                    md2.Col = C32.d2c(0xBFFF0000);
                    md2.Box(sx, sy, num2 * 2, num3 * 2);
                    md2.Col = C32.d2c(0xFF00FF00);
                    md2.Line(sx, sy, tx, ty, 3);
                }

                BLIT.RenderToGLImmediate001(md2, setpass: true);
            }

        rayList.Clear();
        //ray end
        // map start
        if (Configs.configDisplayMapBox.Value) {
            var mdb = new MeshDrawer();
            mdb.activate("map_box", MTRX.MtrMeshNormal, false, C32.d2c(0xFFFFFFFFU));
            // M2FootManager foot = mp.Pr.getFootManager();
            // Console.WriteLine($"{foot.getFootTime()} {foot.vague_foot} {mp.Pr.hasFoot()} {mp.Pr.canJump()}");
            var draw = (BCCLine Bcc, uint col) => {
                float shiftx, shifty;
                Bcc.BCC.getBaseShift(out shiftx, out shifty);
                var flag = mp.Pr.getPhysic().getFootManager().get_FootBCC() == Bcc;
                // float num = mp.Pr.mleft + shiftx;
                // float num2 = mp.Pr.mtop + shifty;
                // float num3 = mp.Pr.mright + shiftx;
                // float num4 = (flag ? mp.Pr.mbottom : X.Mx(mp.Pr.y, mp.Pr.mbottom - 0.22f)) + shifty;
                if (Bcc.AMapDmg != null)
                    foreach (var ditem in Bcc.AMapDmg) {
                        mdb.Col = C32.d2c(0xFFFF7F00U);
                        var extendx = 0.14f;
                        var extendy = Bcc._xd != 0 || (!flag && Bcc.foot_aim == AIM.B) ? -0.23f : extendx;
                        var l = ditem.x - extendx;
                        var r = ditem.right + extendx;
                        var u = ditem.y - extendy;
                        var d = ditem.bottom + extendy;
                        toScreen(ref l, ref u);
                        toScreen(ref r, ref d);
                        mdb.Line(l, u, l, d, 2);
                        mdb.Line(l, d, r, d, 2);
                        mdb.Line(r, d, r, u, 2);
                        mdb.Line(r, u, l, u, 2);
                    }

                {
                    var l = Bcc.x - 0.18f;
                    var r = Bcc.right + 0.18f;
                    var u = Bcc.y - 0.18f;
                    var d = Bcc.bottom + 0.18f;
                    toScreen(ref l, ref u);
                    toScreen(ref r, ref d);
                    mdb.Col = C32.d2c(col);
                    mdb.Line(l, u, l, d, 2);
                    mdb.Line(l, d, r, d, 2);
                    mdb.Line(r, d, r, u, 2);
                    mdb.Line(r, u, l, u, 2);
                }
                float sx = Bcc.sx, sy = Bcc.sy, dx = Bcc.dx, dy = Bcc.dy;
                toScreen(ref sx, ref sy);
                toScreen(ref dx, ref dy);
                mdb.Col = C32.d2c(0xFF007FFFU);
                mdb.Line(sx, sy, dx, dy, 4);
                mdb.Col = C32.d2c(0xFF0000FFU);
                mdb.Box(sx, sy, 8, 8);
                mdb.Box(dx, dy, 8, 8);
            };
            foreach (var bccLine in mp.BCC.getLineVectorLift() ?? [])
                draw(bccLine, 0xFF7FFF00);
            foreach (var bccLinese in mp.BCC.getLineVector() ?? [])
            foreach (var bccLine in bccLinese)
                draw(bccLine, 0xFFCFFF00);
            BLIT.RenderToGLImmediate001(mdb, setpass: true);
        }
        // map end

        if (Configs.configDisplayPlayer.Value && Utils.getPR() != null) renderPlayer(Utils.getPR());
        // bounding box start
        if (Configs.configDisplayBoundingBox.Value) {
            var md = new MeshDrawer();
            md.activate("bounding_box_refresh", MTRX.MtrMeshNormal, false, C32.d2c(0xFFFFFFFFU));
            for (var mvc = 0; mvc < mp.mover_count; mvc++) {
                var mover = mp.getMv(mvc);
                if (mover == null) continue;
                // Console.WriteLine("found mover " + mover);
                if (mover.getColliderCreator() == null) continue;
                var cld = mover.getColliderCreator().Cld;
                if (cld == null) continue;
                // Console.WriteLine("found cld with count "+cld.pathCount);
                for (var pcnt = 0; pcnt < cld.pathCount; pcnt++) {
                    var v = cld.GetPath(pcnt);
                    if (v == null) continue;
                    float baseX = mover.x, baseY = mover.y;
                    toScreen(ref baseX, ref baseY);
                    md.Col = C32.d2c(0xFF66CCFFU);
                    if (mover is PR pr) {
                        if (pr.isNoDamageActive() || !pr.can_applydamage_state())
                            md.Col = C32.d2c(0xFFFFFF00U);
                        if (pr.Skill.ShE.parry_t > 0)
                            md.Col = C32.d2c(0xFF00FF00U);
                    }

                    var n = v.Length;
                    for (var i = 0; i < n; i++) {
                        var j = (i + 1) % n;
                        var d = 64f;
                        md.Line(baseX + v[i].x * d, baseY + v[i].y * d, baseX + v[j].x * d, baseY + v[j].y * d,
                            3);
                    }

                    md.Col = C32.d2c(0xFFEE0000U);
                    md.Box(baseX, baseY, 4, 4);
                }
            }

            GL.LoadProjectionMatrix(JCon.CameraProjectionTransformed);
            BLIT.RenderToGLImmediate001(md, md.draw_triangle_count, setpass: true);
        }

        if (Configs.configDisplayMagicItem.Value) {
            var mgc = m2d.MGC;
            var mgs = Utils.getField<MagicItem[]>(mgc, "AItems");
            MeshDrawer mdGraphics = new(), mdText = new();
            mdGraphics.activate("magic_dumper_graphics", MTRX.MtrMeshNormal, false, C32.d2c(0xFFFFFFFFU));
            mdText.activate("magic_dumper_text", UnifontRenderer.glyphMaterial, false, C32.d2c(0xFFFFFFFFU));
            for (var i = 0; i < mgs.Length; i++) {
                var mg = mgs[i];
                if (mg == null || mg.kind == MGKIND.NONE || mg.closed || mg.killed) continue;
                Console.WriteLine($"found magic {mg}");
                float x = mg.sx, y = mg.sy;
                toScreen(ref x, ref y);
                mdGraphics.Col = C32.d2c(0xFFFF00FF);
                mdGraphics.Rect(x, y, 3, 3);
                var c = 0xFFFFFFFF;
                if (mg.closed && mg.killed) c = 0x3FFFFFFF;
                else if (mg.closed) c = 0x7F7FFF7F;
                else if (mg.killed) c = 0x7FFF7F7F;
                mdText.Col = C32.d2c(c);
                UnifontRenderer.draw(mdText, $"MagicItem #{i} / {mg.kind}", x, y, 1, 1);
                UnifontRenderer.draw(mdText, $"Caster {mg.Caster}", x, y + 16, 1, 1);
                UnifontRenderer.draw(mdText, $"casttime {mg.casttime} / t {mg.t}", x, y + 32, 1, 1);
                UnifontRenderer.draw(mdText, $"phase {mg.phase} / type {mg.type}", x, y + 48, 1, 1);
                UnifontRenderer.draw(mdText, $"(sx, sy) = ({mg.sx}, {mg.sy}) / (dx, dy) = ({mg.dx}, {mg.dy})", x,
                    y + 64, 1, 1);
                UnifontRenderer.draw(mdText, $"sa {mg.sa} / da {mg.da} / sz {mg.sz} / dz {mg.dz}", x, y + 80, 1, 1);
                var fnRunMain = Utils.getField<MagicItem.FnMagicRun>(mg, "fnRunMain");
                UnifontRenderer.draw(mdText, $"fnRunMain {fnRunMain.Method.DeclaringType} : {fnRunMain.Method}", x,
                    y + 96, 1, 1);
            }

            GL.LoadProjectionMatrix(JCon.CameraProjectionTransformed);
            BLIT.RenderToGLImmediate001(mdGraphics, setpass: true);
            BLIT.RenderToGLImmediate001(mdText, setpass: true);
        }

        if (Configs.configDisplayMist.Value) {
            MeshDrawer mdGraphics = new(), mdText = new();
            mdText.activate("mist_dumper_text", UnifontRenderer.glyphMaterial, false, C32.d2c(0xFFFFFFFFU));
            mdGraphics.activate("mist_dumper_graphics", MTRX.MtrMeshNormal, false, C32.d2c(0xFFFFFFFFU));
            var mistManager = m2d.MIST;
            var AGen = Utils.getField<List<MistManager.MistGenerator>>(mistManager, "AGen");
            if (AGen != null) {
                Dictionary<Vector2Int, ulong> dict = new();
                var pr = Utils.getPR();
                float prscreenx = pr?.x ?? 0, prscreeny = pr?.y ?? 0;
                toScreen(ref prscreenx, ref prscreeny);
                for (var ti = 0; ti < AGen.Count; ti++) {
                    var generator = AGen[ti];
                    var k = generator.K;
                    var mistDrawer = k.Msd;
                    if (mistDrawer == null) continue;
                    var lst = mistDrawer.APoint;
                    var rev = lst;
                    var results = k?.AAtk
                        ?.Select(atkItem => atkItem?.SerDmg?.getRawObject())
                        ?.Where(serDmg => serDmg != null)
                        ?.SelectMany(serDmg => serDmg)
                        ?.Aggregate(
                            new StringBuilder(),
                            (sb, kvp) => sb.Append($"[{kvp.Key}'{kvp.Value}]"),
                            sb => sb.ToString()
                        ) ?? "none";
                    UnifontRenderer.draw(mdText,
                        $"#{ti} bitmask {1UL << ti} type {k.type} {results} amount {generator.amount}",
                        prscreenx - 120, prscreeny + 160 + 16 * ti, 1, 1, true);
                    if (rev != null)
                        foreach (var mstpt in rev) {
                            if (mstpt.active == false) continue;
                            var v2i = new Vector2Int(mstpt.x, mstpt.y);
                            if (!dict.ContainsKey(v2i)) dict.Add(v2i, 0UL);
                            dict[v2i] = dict[v2i] | (1UL << ti);
                        }
                }

                foreach (var kvp in dict) {
                    var clenb = mp.CLENB;
                    mdGraphics.Col = C32.d2c(0x7FFFFFFFU);
                    float screenx = kvp.Key.x, screeny = kvp.Key.y;
                    toScreen(ref screenx, ref screeny);
                    mdGraphics.Line(screenx, screeny, screenx, screeny - clenb, 2);
                    mdGraphics.Line(screenx, screeny, screenx + clenb, screeny, 2);
                    mdGraphics.Line(screenx + clenb, screeny, screenx + clenb, screeny - clenb, 2);
                    mdGraphics.Line(screenx, screeny - clenb, screenx + clenb, screeny - clenb, 2);
                    UnifontRenderer.draw(mdText, $"{kvp.Value}", screenx + clenb / 2, screeny - clenb / 2, 1, 1, true);
                }
            }

            GL.LoadProjectionMatrix(JCon.CameraProjectionTransformed);
            BLIT.RenderToGLImmediate001(mdText, setpass: true);
            BLIT.RenderToGLImmediate001(mdGraphics, setpass: true);
        }

        var md3 = new MeshDrawer();
        md3.activate("test", UnifontRenderer.glyphMaterial, false, C32.d2c(0xFFFFFFFF));
        // md3.Box(0, 0, 56, 56);
        md3.Col = C32.d2c(0xFF000000U);
        md3.setCurrentMatrix(Matrix4x4.Scale(new Vector3(1, -1, 1)));
        UnifontRenderer.draw(md3, "ADVANCED mode 2", -26, -8, 1, -1);
        UnifontRenderer.glyphMaterial.SetPass(0);
        GL.LoadProjectionMatrix(JCon.CameraProjectionTransformed);
        BLIT.RenderToGLImmediate001(md3);
    }

    [HarmonyPatch(typeof(M2MovRenderContainer), "RenderWholeMover")]
    [HarmonyPostfix]
    public static void renderWholeMover(ref ProjectionContainer JCon, ref Camera Cam, ref int draw_id,
        ref List<M2RenderTicket>[] ___AADob) {
        if (!isAdvancedEnabled()) return;
        if (Cam.name != Configs.configAdvancedBoxLayer.Value) return;
        if (Configs.configAdvancedBoxMode.Value == 2)
            renderWholeMoverMode2(ref JCon, ref Cam, ref draw_id, ref ___AADob);
        else
            Legacy.renderWholeMoverMode1(ref JCon, ref Cam, ref draw_id, ref ___AADob);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(IN), "run")]
    public static void onINRun(ref bool __runOriginal) {
        if (!isAdvancedEnabled()) return;
        if (!GenshinInCradle.paused) passedFrames++;
        nowTicks = DateTime.Now.Ticks - lastTicks;
        lastTicks += nowTicks;
        if (GenshinInCradle.paused)
            __runOriginal = false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Map2d), "run")]
    public static void onMap2dRun() {
        if (!isAdvancedEnabled()) return;
        lastGt = Map2d.TS;
        passedGt += lastGt;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(IN), "LoggerStbAdd")]
    public static void onLoggerStbAdd(STB Stb) {
        if (!isAdvancedEnabled()) return;
        var mspt = nowTicks / 1e4;
        Stb.Add(" " + Application.targetFrameRate + " " + passedFrames + " " + mspt + " " + (int)(1000 / mspt) + " " +
                lastGt + " " + passedGt);
        PR pr = (NelM2DBase.Instance as NelM2DBase)?.getPrNoel();
        SeedSet.randCounts.TryGetValue(X.Xors, out var cnt0);
        long cnt1 = SeedSet.randCount;
        Stb.Add($" {cnt0} {cnt1}");
        if (pr != null) Stb.Add($" {pr.x:F6} {pr.y:F6}");
        if (GenshinInCradle.paused)
            Stb.Add(" ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff");
        Utils.setStaticField(typeof(Logger), "view_fps_t", 0f);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Map2d), "setTimeScale")]
    public static void onSetTimeScale(ref float v, bool force) {
        if (!isAdvancedEnabled()) return;
        if (GenshinInCradle.paused)
            v = 0;
    }

    public static void renderPlayer(PR pr) {
        if (!isAdvancedEnabled()) return;
        var skill = pr.Skill;
        var s = skill.ShE.Shield;
        var pow = Utils.getField<float>(s, "pow");
        var appear = s.appearable_time;
        var ratio = pow / appear;
        float bx = pr.x, by = pr.y;
        toScreen(ref bx, ref by);
        // Console.WriteLine($"b {bx} {by}");
        MeshDrawer mdGraphics = new(), mdText = new();
        mdGraphics.activate("render_player_graphics", MTRX.MtrMeshNormal, false, C32.d2c(0xFFFFFFFF));
        mdText.activate("render_player_text", UnifontRenderer.glyphMaterial, false, C32.d2c(0xFFFFFFFF));
        drawBox(mdGraphics, bx, by + 80, pow >= 0 ? 1 - ratio : -ratio, C32.d2c(pow >= 0 ? 0xFF66CCFF : 0xFFEE0000));
        var MKBurst = Utils.getField<MKind>(skill.getBurstSelector(), "MKBurst");
        var faint = MDAT.calcBurstFaintedRatio(pr, MKBurst, skill.getBurstSelector().execute_count,
            pr.Ser.burstConsumeRatio());
        drawBox(mdGraphics, bx, by + 88, Configs.configBurstFaintReverse.Value ? 1 - faint : faint,
            C32.d2c(faintColor(faint)));
        mdText.Col = new Color32(255, 255, 255, 255);
        UnifontRenderer.draw(mdText, $"faint {faint * 100.0:F2}% {appear - pow:F2}", bx - 80, by + 116, 1, 1, true);
        UnifontRenderer.draw(mdText, $"state {pr.get_current_state()} {pr.get_state_time():F2}", bx - 80, by + 132, 1,
            1, true);
        if (pr.Skill.getCurMagic() is { } curmg)
            UnifontRenderer.draw(mdText, $"current magic {curmg.kind} {curmg.phase} {curmg.t:F2}", bx - 80, by + 100, 1,
                1, true);
        else
            UnifontRenderer.draw(mdText, "current magic not present", bx - 80, by + 100, 1, 1, true);
        UnifontRenderer.draw(mdText, $"x=({pr.x:F6}, {pr.y:F6}); v=({pr.vx:F6}, {pr.vy:F6})", bx - 80, by + 148, 1, 1,
            true);
        double num = 6f;
        if (!(pr.Mp.floort < pr.sink_ignore_skill_effect)) {
            double re = pr.getRE(RCP.RPI_EFFECT.SINK_REDUCE);
            if (re < 0) num = Math.Max(1, (1 + re) * num);
            else if (re > 0) num += 90.0 * Math.Pow(Math.Min(1, re), 2);
        }

        var es = pr.RCenemy_sink.val;
        drawBox(mdGraphics, bx, by + 96, (float)Math.Abs(es / num), C32.d2c(es >= 0 ? 0xFFCC66FF : 0xFF66FFCC));
        UnifontRenderer.draw(mdText, $"sink={es:F6} / {num:F6}", bx - 80, by + 164, 1, 1, true);
        for (var i = 0; i < 14; i++) {
            var ok = pr.isNoDamageActive((NDMG)i);
            var x = (i >= 7 ? 1 : -1) * 28;
            var y = (3 - i % 7) * 12 + 18;
            mdGraphics.Col = C32.d2c(ok ? 0xFF00FF00 : 0xFFFF0000);
            mdGraphics.Box(bx + x, by + y, 8, 8);
        }

        mdGraphics.Col = C32.d2c(pr.isNoDamageActive() ? 0xFF00FF00 : 0xFFFF0000);
        mdGraphics.Circle(bx - 28, by - 42, 6);
        mdGraphics.Col = C32.d2c(!pr.can_applydamage_state() ? 0xFF00FF00 : 0xFFFF0000);
        mdGraphics.Circle(bx + 28, by - 42, 6);

        BLIT.RenderToGLImmediate001(mdGraphics, setpass: true);
        BLIT.RenderToGLImmediate001(mdText, setpass: true);
    }

    private static uint faintColor(float f) {
        return f == 0 ? 0xFF00FF00 :
            f <= 0.1 ? 0xFFCCCCFF :
            f <= 0.33 ? 0xFFFFFF00 :
            f <= 0.67 ? 0xFFFF7F00 :
            f < 1 ? 0xFFFF0000 : 0xFFFF7FFF;
    }

    private static void drawBox(MeshDrawer md, float x, float y, float ratio, Color32 color) {
        md.Col = C32.d2c(0xFF000000);
        float len = 86, len2 = 87, t1 = 4, t2 = 8;
        md.Line(x - len2, y, x + len2, y, t2);
        md.Col = color;
        md.Line(x - len, y, x - len + 2 * len * ratio, y, t1);
    }
}