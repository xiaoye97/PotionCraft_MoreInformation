using UnityEngine;
using System.Reflection;
using PotionCraft.DebugObjects.DebugWindows;

namespace xiaoye97
{
    public static class Helper
    {
        /// <summary>
        /// 从嵌入资源加载Texture2D
        /// </summary>
        public static Texture2D LoadResTexture2D(string name)
        {
            var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("PotionCraft_MoreInformation." + name);
            int length = (int)s.Length;
            byte[] bs = new byte[length];
            s.Read(bs, 0, length);
            s.Close();
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bs);
            return tex;
        }

        /// <summary>
        /// 从嵌入资源加载Sprite
        /// </summary>
        public static Sprite LoadSprite(string name)
        {
            var tex = LoadResTexture2D(name);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0));
            return sprite;
        }

        /// <summary>
        /// 创建一个透明的只有文本的Debug窗口
        /// </summary>
        public static DebugWindow CreateClearDebugWindow(string title, Vector2 pos)
        {
            DebugWindow window = DebugWindow.Init(title, true);
            window.colliderBackground.enabled = false;
            window.headTransform.gameObject.SetActive(false);
            window.captionText.gameObject.SetActive(false);
            window.spriteScratches.gameObject.SetActive(false);
            window.spriteBackground.color = Color.clear;
            window.transform.localPosition = new Vector3(pos.x, pos.y, -10);
            return window;
        }
    }
}