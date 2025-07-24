using System;
using System.Collections.Generic;
using Verse;
using UnityEngine;
using HarmonyLib;
using RimWorld.Planet;
using RimWorld;

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
        private const float gizmoSize = 75f;
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
            if (!ScrollableGizmoSettings.doFixVerticalScrollMouseWheel)
                return;

            if (Event.current.type == EventType.ScrollWheel && outRect.Contains(Event.current.mousePosition) && !selected)
            {
                scroll.y += Event.current.delta.y * ScrollableGizmoSettings.scrollSpeed;
                scroll.y = Mathf.Clamp(scroll.y, 0f, viewRect.height);
                Event.current.Use();
            }
        }

        public static void FixVerticalScrollClickAndDrag(Rect outRect, Rect viewRect)
        {
            if (!ScrollableGizmoSettings.doFixVerticalScrollClickAndDrag)
                return;

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

        public static void DrawGizmoBackground(Rect outRect)
        {
            if (!ScrollableGizmoSettings.drawBackground)
                return;

            Widgets.DrawWindowBackground(outRect);
        }

        public static void UpdateScrollPosition()
        {
            if (ScrollableGizmoSettings.startScrollAtBottom && (heightDrawnRecently != GizmoGridDrawer.HeightDrawnRecently))
                scroll.y = GizmoGridDrawer.HeightDrawnRecently - bottomOffset + arbitraryOffset;
        }

        public static bool IsInWorldMenu()
        {
            return Find.World.renderer.wantedMode == WorldRenderMode.Planet;
        }

        public static bool IsInArchitectMenu()
        {
            return Find.MainTabsRoot.OpenTab == MainButtonDefOf.Architect;
        }

        public static bool CanDoScrollableGizmos()
        {
            return ((ScrollableGizmoSettings.architectMenuOnly && IsInArchitectMenu()) || !ScrollableGizmoSettings.architectMenuOnly) && !IsInWorldMenu();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GizmoGridDrawer), nameof(GizmoGridDrawer.DrawGizmoGrid))]
        public static void GizmoGridDrawerPatchPrefix(IEnumerable<Gizmo> gizmos, ref float startX, ref Gizmo mouseoverGizmo, Func<Gizmo, bool> customActivatorFunc, Func<Gizmo, bool> highlightFunc, Func<Gizmo, bool> lowlightFunc, bool multipleSelected)
        {
            if (!CanDoScrollableGizmos())
                return;

            foreach (Gizmo gizmo in gizmos)
            {
                Command command = gizmo as Command;
                if (command == null) continue;
                command.shrinkable = false;
            }

            startX -= ScrollableGizmoSettings.outWidthOffset;

            // get heights
            float viewHeight = GizmoGridDrawer.HeightDrawnRecently - bottomOffset + arbitraryOffset;
            float outHeight = Mathf.Min(viewHeight, ScrollableGizmoSettings.outHeight + arbitraryOffset);

            // create rects
            Rect gizmoOut = new Rect(
                startX - arbitraryOffset + ScrollableGizmoSettings.outWidthOffset,
                UI.screenHeight - outHeight - bottomOffset,
                UI.screenWidth - sideOffset - startX + scrollBarWidth + arbitraryOffset,
                outHeight);

            Rect gizmoView = new Rect(
                startX - arbitraryOffset,
                UI.screenHeight - viewHeight - (gizmoSize / 2f),
                UI.screenWidth - sideOffset - scrollBarWidth - startX + arbitraryOffset,
                viewHeight);

            // hacky scroll fix
            FixVerticalScrollMouseWheel(gizmoOut, gizmoView);
            FixVerticalScrollClickAndDrag(gizmoOut, gizmoView);

            // draw background
            DrawGizmoBackground(gizmoOut);

            // start scroll
            Widgets.BeginScrollView(gizmoOut, ref scroll, gizmoView, ScrollableGizmoSettings.showScrollBar);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GizmoGridDrawer), nameof(GizmoGridDrawer.DrawGizmoGrid))]
        public static void GizmoGridDrawerPatchPostfix()
        {
            if (!CanDoScrollableGizmos())
                return;

            Widgets.EndScrollView();
            UpdateScrollPosition();
            heightDrawnRecently = GizmoGridDrawer.HeightDrawnRecently;
        }
    }
}
