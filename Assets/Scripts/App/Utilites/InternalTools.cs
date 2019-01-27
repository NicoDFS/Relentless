using DG.Tweening;
using Loom.ZombieBattleground.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace Loom.ZombieBattleground.Helpers
{
    public static class InternalTools
    {
        public static void SetLayerRecursively(
            GameObject parent, int layer, List<string> ignoreNames = null, bool parentIgnored = false)
        {
            if (!parentIgnored)
            {
                parent.layer = layer;
            }

            bool ignored = false;
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                if (ignoreNames != null)
                {
                    ignored = ignoreNames.Contains(parent.transform.GetChild(i).gameObject.name);
                }

                if (!ignored && !parentIgnored || ignoreNames == null)
                {
                    parent.transform.GetChild(i).gameObject.layer = layer;
                }

                if (parent.transform.GetChild(i).childCount > 0)
                {
                    SetLayerRecursively(parent.transform.GetChild(i).gameObject, layer, ignoreNames, ignored);
                }
            }
        }

        public static void ShuffleList<T>(this IList<T> list)
        {
            Random rnd = new Random();
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = rnd.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static void GroupHorizontalObjects(Transform root, float offset, float spacing, float offsetY, bool isReverse = false, float offsetZ = 0f)
        {
            int count = root.childCount;

            float width = spacing * (count - 1);

            Vector3 pivot = new Vector3(offset, 0, 0);

            for (int i = 0; i < count; i++)
            {
                root.GetChild(i).localPosition = new Vector3(pivot.x - width / 2f, offsetY, offsetZ);
                pivot.x += width / (count-1);
            }
        }

        public static void GroupVerticalObjects(
            Transform root, float spacing, float centerOffset = -7f, float height = 7.2f)
        {
            int count = root.childCount;
            float halfHeightOffset = height + spacing;

            float startPos = centerOffset + (count - 1) * halfHeightOffset / 2f;

            for (int i = 0; i < count; i++)
            {
                root.GetChild(i).localPosition = new Vector3(root.GetChild(i).localPosition.x,
                    startPos - halfHeightOffset * i, root.GetChild(i).localPosition.z);
            }
        }

        public static List<T> GetRandomElementsFromList<T>(IList<T> list, int count)
        {
            List<T> shuffledList = new List<T>(count);
            shuffledList.AddRange(list);

            if (list.Count <= count)
                return shuffledList;

            ShuffleList(shuffledList);
            return shuffledList.GetRange(0, count);
        }

        public static float DeviceDiagonalSizeInInches()
        {
            float screenWidth = Screen.width / Screen.dpi;
            float screenHeight = Screen.height / Screen.dpi;
            float diagonalInches = Mathf.Sqrt(Mathf.Pow(screenWidth, 2) + Mathf.Pow(screenHeight, 2));

            return diagonalInches;
        }

        public static bool IsTabletScreen()
        {
#if FORCE_TABLET_UI
            return true;
#elif FORCE_PHONE_UI
            return false;
#else
            return DeviceDiagonalSizeInInches() > 6.5f;
#endif
        }

        public static string ProccesEnumToString(string origin)
        {
            if (string.IsNullOrEmpty(origin))
                return origin;

            char[] chars = origin.Replace("_", Constants.Space).ToCharArray();

            string newValue = chars[0].ToString().ToUpperInvariant();

            for(int i = 1; i < chars.Length; i++)
            {
                if (char.IsUpper(chars[i]))
                {
                    newValue += Constants.Space;
                }

                newValue += chars[i].ToString().ToLowerInvariant();
            }
          
            return newValue;
        }

        public static Sequence DoActionDelayed(TweenCallback action, float delay = 0f)
        {
            if (action == null)
                return null;

            Sequence sequence = DOTween.Sequence();
            sequence.PrependInterval(delay);
            sequence.OnComplete(action);

            return sequence;
        }

        public static string FormatStringToPascaleCase(string root)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(root.ToLower().Replace("_", " ")).Replace(" ", string.Empty);
        }
    }
}
