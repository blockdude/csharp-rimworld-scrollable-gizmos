using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;

namespace ScrollableGizmos
{
    public class GizmoSettings : ModSettings
    {
        public static bool enabled = true;
        public static bool showScrollBar = true;
        public static float outHeight = 160;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enabled, "enabled");
            Scribe_Values.Look(ref showScrollBar, "showscrollbar");
            Scribe_Values.Look(ref outHeight, "outheight");
            base.ExposeData();

            // patch and unpatch
            if (enabled && !GizmoPatch.patched)
            {
                GizmoPatch.harmony.PatchAll();
                GizmoPatch.patched = true;
            }
            else if (!enabled && GizmoPatch.patched)
            {
                GizmoPatch.harmony.UnpatchAll(GizmoPatch.harmony.Id);
                GizmoPatch.patched = false;
            }
        }
    }

    public class GizmoSettingsMod : Mod
    {
        GizmoSettings settings;
        string buffer;

        public GizmoSettingsMod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<GizmoSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("Enable Scrollable Gizmos", ref GizmoSettings.enabled);
            listingStandard.CheckboxLabeled("Show Scrollbar", ref GizmoSettings.showScrollBar);
            listingStandard.TextFieldNumericLabeled<float>("Scroll View height (increments of 80 look best)                                   ", ref GizmoSettings.outHeight, ref buffer);
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Scrollable Gizmos";
        }
    }
}
