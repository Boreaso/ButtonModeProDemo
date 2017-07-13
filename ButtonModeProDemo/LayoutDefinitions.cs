using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace BoardShow
{
    #region 枚举类型
    /// <summary>
    /// 按键显示的类型
    /// </summary>
    public enum BoardShowMode
    {
        自选按钮 = 0,
        常规字母 = 1
    }

    /// <summary>
    /// 按键显示主题
    /// </summary>
    public enum BoardThemeType
    {
        简约风格 = 0,
        立体风格 = 1,
        斜角风格 = 2
    }

    /// <summary>
    /// 键盘布局类型
    /// </summary>
    public enum BoardLayoutType
    {
        单面显示,
        双面水平,
        双面垂直
    }
    #endregion

    #region 键盘布局定义类
    /// <summary>
    /// 每个按键的定义
    /// </summary>
    public class ButtonDefinition
    {
        /// <summary>
        /// 按键码
        /// </summary>
        public int KeyCode = -1;

        /// <summary>
        /// 按键文本
        /// </summary>
        public string KeyText = "";

        /// <summary>
        /// 按键的位置
        /// </summary>
        public Rect ButtonRect;

        public ButtonDefinition()
        {
        }

        public ButtonDefinition(int keyCode, string keyText, Rect buttonRect)
        {
            this.KeyCode = keyCode;
            this.KeyText = keyText;
            this.ButtonRect = buttonRect;
        }

        public bool Compare(ButtonDefinition targetDefinition)
        {
            if (targetDefinition.KeyCode != this.KeyCode ||
               targetDefinition.KeyText != this.KeyText)
            {
                return false;
            }

            if (targetDefinition.ButtonRect != this.ButtonRect)
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 键盘布局定义
    /// </summary>
    public class BoardLayout
    {
        /// <summary>
        /// 键盘布局名称
        /// </summary>
        public string LayoutName;

        /// <summary>
        /// 键盘1布局定义
        /// </summary>
        public List<ButtonDefinition> BoardButtonDefs;

        public BoardLayout()
        {
            this.BoardButtonDefs = new List<ButtonDefinition>();
        }

        public BoardLayout(string layoutName, List<ButtonDefinition> boardButtonDefs)
        {
            this.LayoutName = layoutName;
            this.BoardButtonDefs = boardButtonDefs;
        }

        /// <summary>
        /// BoardLayout深拷贝
        /// </summary>
        /// <param name="boardLayout"></param>
        public BoardLayout Clone()
        {
            BoardLayout boardLayout = new BoardLayout();

            boardLayout.LayoutName = this.LayoutName;

            foreach (ButtonDefinition buttonDef in this.BoardButtonDefs)
            {
                boardLayout.BoardButtonDefs.Add(new ButtonDefinition(buttonDef.KeyCode, buttonDef.KeyText, buttonDef.ButtonRect));
            }

            return boardLayout;
        }

        public bool Compare(BoardLayout targetLayout)
        {
            if (targetLayout.LayoutName != this.LayoutName)
            {
                return false;
            }

            for (int i = 0; i < this.BoardButtonDefs.Count; i++)
            {
                if (!targetLayout.BoardButtonDefs[i].Compare(this.BoardButtonDefs[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
    #endregion
}
