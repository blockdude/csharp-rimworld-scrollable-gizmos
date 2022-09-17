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

namespace ScrollableGizmos
{
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(GizmoGridDrawer), nameof(GizmoGridDrawer.DrawGizmoGrid))]
    class ScrollableGizmoPatch
    {
        public static Harmony harmony;
        public static bool patched;
        static ScrollableGizmoPatch()
        {
            harmony = new Harmony("Blockdude.ScrollableGizmos");

            if (ScrollableGizmoSettings.enabled)
            {
                harmony.PatchAll();
                patched = true;
            }
            else
            {
                patched = false;
            }
        }

        // gizmo scroll
        private static Vector2 scroll = new Vector2();

        // some constants found in gizmo code
        private const float scrollBarOffset = 16f;
        private const float sideOffset = 147f;
        private const float bottomOffset = 35f;
        private const float buttonSize = 75f;

        // lists for gizmo count
        private static List<Gizmo> tmpAllGizmos = new List<Gizmo>();
        private static List<List<Gizmo>> gizmoGroups = new List<List<Gizmo>>();

        public static void GetGizmos(IEnumerable<Gizmo> gizmos)
        {
            /*
             * Begining of copied code so i can count gizmos
             * I should probably change this because it is unneeded overhead
             */
            tmpAllGizmos.Clear();
			gizmoGroups.Clear();
			tmpAllGizmos.AddRange(gizmos);
			for (int i = 0; i < tmpAllGizmos.Count; i++)
			{
				Gizmo gizmo = tmpAllGizmos[i];
				bool flag = false;
				for (int j = 0; j < gizmoGroups.Count; j++)
				{
					if (gizmoGroups[j][0].GroupsWith(gizmo))
					{
						flag = true;
						gizmoGroups[j].Add(gizmo);
						gizmoGroups[j][0].MergeWith(gizmo);
						break;
					}
				}
				if (!flag)
				{
					List<Gizmo> list = SimplePool<List<Gizmo>>.Get();
					list.Add(gizmo);
					gizmoGroups.Add(list);
				}
			}
            /*
             * End of copied code
             */
        }

        public static float CalculateViewHeight(float startX)
        {
            float gizmoSpacing = GizmoGridDrawer.GizmoSpacing.x;
            float viewWidth = UI.screenWidth - startX - sideOffset;

            float widthTracker = 0;
            int rowCount = 1;
            for (int i = 0; i < gizmoGroups.Count; i++)
            {
                if (!gizmoGroups[i][0].Visible)
                    continue;

                // get width of gizmo
                float gizmoWidth = gizmoGroups[i][0].GetWidth(float.MaxValue);
                widthTracker += gizmoWidth;
                widthTracker += gizmoSpacing;

                if (widthTracker > viewWidth)
                {
                    rowCount++;
                    widthTracker = gizmoWidth;
                }
            }

            float viewHeight = rowCount * (buttonSize + gizmoSpacing);
            return viewHeight;
        }

        // track if the scroll bar is being clicked and dragged
        private static bool selected = false;

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

            Rect scrollBarArea = new Rect(outRect.x + outRect.width - scrollBarOffset, outRect.y, scrollBarOffset, outRect.height);
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

        public static void Prefix(IEnumerable<Gizmo> gizmos, ref float startX)
        {
            UI.screenWidth += ScrollableGizmoSettings.outWidthOffset;
            GetGizmos(gizmos);

            // calulate rect heights for scroll view (i add 10 so tops don't get cut off)
            float viewHeight = CalculateViewHeight(startX) + 10f;
            float outHeight = Mathf.Min(viewHeight, ScrollableGizmoSettings.outHeight) + 10f;

            // create rects
            Rect gizmoOut = new Rect(startX, UI.screenHeight - outHeight - bottomOffset, UI.screenWidth - sideOffset - startX + scrollBarOffset, outHeight);
            Rect gizmoView = new Rect(startX, UI.screenHeight - viewHeight, UI.screenWidth - sideOffset - scrollBarOffset - startX, viewHeight);
            Rect gizmoGroup = new Rect(0f, bottomOffset, UI.screenWidth, UI.screenHeight - bottomOffset);

            // hacky scroll fix
            if (ScrollableGizmoSettings.doFixVerticalScrollMouseWheel) FixVerticalScrollMouseWheel(gizmoOut, gizmoView);
            if (ScrollableGizmoSettings.doFixVerticalScrollClickAndDrag) FixVerticalScrollClickAndDrag(gizmoOut, gizmoView);

            // draw background
            if (ScrollableGizmoSettings.drawBackground) Widgets.DrawWindowBackground(gizmoOut);

            // start scroll
            Widgets.BeginScrollView(gizmoOut, ref scroll, gizmoView, ScrollableGizmoSettings.showScrollBar);
            GUI.BeginGroup(gizmoGroup);
        }

        public static void Postfix()
        {
            GUI.EndGroup();
            Widgets.EndScrollView();
            UI.screenWidth -= ScrollableGizmoSettings.outWidthOffset;
        }
    }
}
