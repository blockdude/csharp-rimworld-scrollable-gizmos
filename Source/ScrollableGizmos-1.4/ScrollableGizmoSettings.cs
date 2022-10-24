using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;

namespace ScrollableGizmos
{
    public class ScrollableGizmoSettings : ModSettings
    {
        public static bool enabled = true;

        public static bool doFixVerticalScrollMouseWheel = true;
        public static bool doFixVerticalScrollClickAndDrag = true;

        public static float outHeight = 180;
        public static int outWidthOffset = -16;
        public static float scrollSpeed = 13.33f;

        public static bool showScrollBar = true;
        public static bool drawBackground = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enabled, "enabled");
            Scribe_Values.Look(ref showScrollBar, "showScrollBar");
            Scribe_Values.Look(ref outHeight, "outHeight");
            Scribe_Values.Look(ref outWidthOffset, "outWidthOffset");
            Scribe_Values.Look(ref scrollSpeed, "scrollSpeed");
            Scribe_Values.Look(ref doFixVerticalScrollMouseWheel, "doFixVerticalScrollMouseWheel");
            Scribe_Values.Look(ref doFixVerticalScrollClickAndDrag, "doFixVerticalScrollClickAndDrag");
            Scribe_Values.Look(ref drawBackground, "drawBackground");
            base.ExposeData();

            // patch and unpatch
            if (enabled && !ScrollableGizmoPatch.patched)
            {
                ScrollableGizmoPatch.harmony.PatchAll();
                ScrollableGizmoPatch.patched = true;
            }
            else if (!enabled && ScrollableGizmoPatch.patched)
            {
                ScrollableGizmoPatch.harmony.UnpatchAll(ScrollableGizmoPatch.harmony.Id);
                ScrollableGizmoPatch.patched = false;
            }
        }
    }

    public class ScrollableGizmoSettingsMod : Mod
    {
        ScrollableGizmoSettings settings;
        string bufferOutHeight;
        string bufferOutWidth;
        string bufferScrollSpeed;

        public ScrollableGizmoSettingsMod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<ScrollableGizmoSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("Enable scrollable gizmos", ref ScrollableGizmoSettings.enabled);
            listingStandard.CheckboxLabeled("Show scrollbar", ref ScrollableGizmoSettings.showScrollBar);
            listingStandard.CheckboxLabeled("Try and fix scrolling mouse wheel (scroll speed needs this enabled to work) (default: enabled)", ref ScrollableGizmoSettings.doFixVerticalScrollMouseWheel);
            listingStandard.CheckboxLabeled("Try and fix scrolling click and drag (default: enabled)", ref ScrollableGizmoSettings.doFixVerticalScrollClickAndDrag);
            listingStandard.TextFieldNumericLabeled("Scroll view height (default: 180)                                                          ", ref ScrollableGizmoSettings.outHeight, ref bufferOutHeight);
            listingStandard.TextFieldNumericLabeled("Scroll speed (default: 13.33)                                                               ", ref ScrollableGizmoSettings.scrollSpeed, ref bufferScrollSpeed, min: float.MinValue, max: float.MaxValue);
            listingStandard.TextFieldNumericLabeled("Scroll view width offset (possible ui bugs if changed) (default: -16)    ", ref ScrollableGizmoSettings.outWidthOffset, ref bufferOutWidth, min: float.MinValue, max: float.MaxValue);
            listingStandard.CheckboxLabeled("Draw background (work in progress)", ref ScrollableGizmoSettings.drawBackground);
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Scrollable Gizmos";
        }
    }
}
