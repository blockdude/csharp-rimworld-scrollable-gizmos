using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.Steam;
using UnityEngine;
using HarmonyLib;
using RimWorld.Planet;

namespace ScrollableGizmos
{
    [StaticConstructorOnStartup]
    [HarmonyPatch]
    class ScrollableGizmoPatch
    {
        public static Harmony harmony;
        public static bool patched;
        static ScrollableGizmoPatch()
        {
            harmony = new Harmony("Blockdude.ScrollableGizmos");
            if (ScrollableGizmoSettings.enabled)
                harmony.PatchAll();
            patched = ScrollableGizmoSettings.enabled;
        }

        // gizmo scroll
        private static Vector2 scroll = new Vector2();

        // some constants found in gizmo code
        private const float scrollBarWidth = 16f;
        private const float sideOffset = 147f;
        private const float bottomOffset = 35f;
        private const float arbitraryOffset = 10f;

        // track if the scroll bar is being clicked and dragged
        private static bool selected = false;

        // testing
        private static float heightDrawnRecently = 0f;

        public static void FixVerticalScrollMouseWheel(Rect outRect, Rect viewRect)
        {
            if (Event.current.type == EventType.ScrollWheel && outRect.Contains(Event.current.mousePosition) && !selected)
            {
                scroll.y += Event.current.delta.y * ScrollableGizmoSettings.scrollSpeed;
                scroll.y = Mathf.Clamp(scroll.y, 0f, viewRect.height);
                Event.current.Use();
            }
        }

        public static void FixVerticalScrollClickAndDrag(Rect outRect, Rect viewRect)
        {
            // don't try and fix if the scroll bar is not even shown
            if (!ScrollableGizmoSettings.showScrollBar)
                return;

            Rect scrollBarArea = new Rect(outRect.x + outRect.width - scrollBarWidth, outRect.y, scrollBarWidth, outRect.height);
            //float scrollBarHeight = (viewRect.height / outRect.height);
            //Rect scrollBar = new Rect(outRect.x + outRect.width - scrollBarOffset, outRect.y, scrollBarOffset, scrollBarHeight);

            if (Event.current.type == EventType.MouseUp && selected)
            {
                selected = false;
                Event.current.Use();
            }

            if ((Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown) && (scrollBarArea.Contains(Event.current.mousePosition) || selected == true))
			{
                selected = true;
                float viewPercent = viewRect.height / outRect.height;
                // i don't know what formula to use to get the size of the scroll bar so i will use this til further notice
                scroll.y = (Event.current.mousePosition.y - scrollBarArea.y - (viewPercent * 2)) * viewPercent;
                scroll.y = Mathf.Clamp(scroll.y, 0f, viewRect.height);
				Event.current.Use();
			}
        }

        public static bool IsInWorldMenu()
        {
            return Find.World.renderer.wantedMode != WorldRenderMode.Planet;
        }

        // hacky and indirect method to disable shrinkable gizmos (should find a different way like patching the command constructor)
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Gizmo), "Visible", MethodType.Getter)]
        public static void GizmoVisiblePatchPrefix(Gizmo __instance)
        {
            Command command = __instance as Command;
            if (command != null) command.shrinkable = false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GizmoGridDrawer), nameof(GizmoGridDrawer.DrawGizmoGrid))]
        public static void GizmoGridDrawerPatchPrefix(ref IEnumerable<Gizmo> gizmos, ref float startX)
        {
            startX -= ScrollableGizmoSettings.outWidthOffset;

            // get heights
            float viewHeight = Mathf.Clamp(GizmoGridDrawer.HeightDrawnRecently - bottomOffset + arbitraryOffset, 0f, UI.screenHeight - bottomOffset);
            float outHeight = Mathf.Min(viewHeight, ScrollableGizmoSettings.outHeight + arbitraryOffset);

            // create rects
            Rect gizmoOut = new Rect(startX - arbitraryOffset + ScrollableGizmoSettings.outWidthOffset, UI.screenHeight - outHeight - bottomOffset, UI.screenWidth - sideOffset - startX + scrollBarWidth + arbitraryOffset, outHeight);
            Rect gizmoView = new Rect(startX - arbitraryOffset + ScrollableGizmoSettings.outWidthOffset, UI.screenHeight - viewHeight, UI.screenWidth - sideOffset - scrollBarWidth - startX + arbitraryOffset, viewHeight);
            Rect gizmoGroup = new Rect(ScrollableGizmoSettings.outWidthOffset, bottomOffset, UI.screenWidth, UI.screenHeight - bottomOffset);

            // hacky scroll fix
            if (ScrollableGizmoSettings.doFixVerticalScrollMouseWheel) FixVerticalScrollMouseWheel(gizmoOut, gizmoView);
            if (ScrollableGizmoSettings.doFixVerticalScrollClickAndDrag) FixVerticalScrollClickAndDrag(gizmoOut, gizmoView);

            // draw background
            //MainButtonWorker_ToggleWorld
            if (ScrollableGizmoSettings.drawBackground)
                if (IsInWorldMenu())
                    Widgets.DrawWindowBackground(gizmoOut);

            // start scroll
            Widgets.BeginScrollView(gizmoOut, ref scroll, gizmoView, ScrollableGizmoSettings.showScrollBar);
            Widgets.BeginGroup(gizmoGroup);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GizmoGridDrawer), nameof(GizmoGridDrawer.DrawGizmoGrid))]
        public static void GizmoGridDrawerPatchPostfix()
        {
            Widgets.EndGroup();
            Widgets.EndScrollView();

            // put scroll at bottom when there is a height change
            // this is the best i can do at the moment for detecting change of gizmos
            if (ScrollableGizmoSettings.startScrollAtBottom)
            {
                if (heightDrawnRecently != GizmoGridDrawer.HeightDrawnRecently)
                    scroll.y = GizmoGridDrawer.HeightDrawnRecently - bottomOffset + arbitraryOffset;
            }

            // update cached variables
            heightDrawnRecently = GizmoGridDrawer.HeightDrawnRecently;
        }
    }
}
