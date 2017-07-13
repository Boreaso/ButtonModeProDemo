using BoardShow;
using BoardShow.Controls;
using CalabashTools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ButtonModeProDemo
{
    public class Setting
    {
        public static string BoardLayoutFilesFolder = System.IO.Path.Combine(Environment.CurrentDirectory, Encrypt.TEXT("BoardLayoutDefs"));
        public static string BoardLayoutFileNamePrefix = "BoardLayout";

        public List<BoardLayout> savedBoardLayouts = new List<BoardLayout>();
        public BoardLayout currentBoardLayout = null;

        public Setting()
        {
            this.savedBoardLayouts = this.LoadBoardLayouts();
        }

        public List<BoardLayout> LoadBoardLayouts()
        {

            List<BoardLayout> layoutList = new List<BoardLayout>();
            List<FileInfo> layoutFiles = CalabashTools.FileIO.ListAllFiles(BoardLayoutFilesFolder);

            if (layoutFiles != null)
            {
                foreach (FileInfo layoutFile in layoutFiles)
                {
                    BoardLayout boardLayout = this.LoadBoardLayoutFromJsonFile(layoutFile.Name);
                    layoutList.Add(boardLayout);
                }
            }

            return layoutList;
        }

        public void SaveBoardLayouts()
        {
            this.RemoveAllBoardLayoutFiles();

            lock (this.savedBoardLayouts)
            {
                for (int i = 0; i < this.savedBoardLayouts.Count; i++)
                {
                    string fileName = BoardLayoutFileNamePrefix + i;
                    this.SaveBoardLayoutToJsonFile(fileName, this.savedBoardLayouts[i], true);
                }
            }
        }

        public BoardLayout LoadBoardLayoutFromJsonFile(string fileName)
        {
            string filePath = System.IO.Path.Combine(BoardLayoutFilesFolder, fileName);
            BoardLayout currentLayout = null;

            if (CalabashTools.FileIO.ExistFile(filePath))
            {
                try
                {
                    string content = CalabashTools.FileIO.ReadFromFile(filePath);

                    if (!string.IsNullOrEmpty(content))
                    {
                        currentLayout = JsonConvert.DeserializeObject<BoardLayout>(content);
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return currentLayout;
        }

        public int SaveBoardLayoutToJsonFile(string fileName, BoardLayout boardLayout, bool isOverlay)
        {
            string filePath = System.IO.Path.Combine(BoardLayoutFilesFolder, fileName + ".json");

            string layoutJson = string.Empty;

            try
            {
                layoutJson = JsonConvert.SerializeObject(boardLayout);
            }
            catch (Exception)
            {
                return -1;
            }

            if (!CalabashTools.FileIO.ExistFolder(BoardLayoutFilesFolder))
            {
                CalabashTools.FileIO.CreateFolder(BoardLayoutFilesFolder);
            }

            if (!isOverlay && CalabashTools.FileIO.ExistFile(filePath))
            {
                return -2;
            }
            else
            {
                CalabashTools.FileIO.Write2File(filePath, layoutJson);
            }

            return 0;
        }

        public int SaveBoardLayoutToJsonFile(string fileName, string layoutName, List<DraggableBorder> canvasBorders)
        {
            string filePath = System.IO.Path.Combine(BoardLayoutFilesFolder, fileName + ".json");

            BoardLayout boardLayout = this.BuildBoardLayout(canvasBorders, layoutName);

            if (boardLayout != null)
            {
                this.SaveBoardLayoutToJsonFile(fileName, boardLayout, true);
            }

            return 0;
        }

        public void RemoveAllBoardLayoutFiles()
        {
            foreach (FileInfo fileInfo in CalabashTools.FileIO.ListAllFiles(BoardLayoutFilesFolder))
            {
                CalabashTools.FileIO.DeleteFile(fileInfo.FullName);
            }
        }

        public void RemoveBoardLayoutFile(string fileName)
        {
            string filepath = System.IO.Path.Combine(BoardLayoutFilesFolder, fileName + ".json");
            CalabashTools.FileIO.DeleteFile(filepath);
        }

        public BoardLayout BuildBoardLayout(List<DraggableBorder> canvasBorders, string layoutName = "")
        {
            BoardLayout boardLayout = null;

            List<ButtonDefinition> boardDefs = new List<ButtonDefinition>();

            foreach (DraggableBorder border in canvasBorders)
            {
                int keyCode = border.KeyCode;
                string keyText = border.KeyText;
                Rect buttonRect = border.RectangleGeometry.Rect;
                ButtonDefinition buttonDef = new ButtonDefinition(keyCode, keyText, buttonRect);

                boardDefs.Add(buttonDef);
            }

            if (boardDefs.Count > 0)
            {
                boardLayout = new BoardLayout(layoutName, boardDefs);
            }

            return boardLayout;

        }
    }
}
