// UndoManager.cs
using UnityEngine;
using System.Collections.Generic;

public class UndoManager : MonoBehaviour
{
    public static UndoManager Instance { get; private set; }

    private Stack<ICommand> undoStack = new Stack<ICommand>();
    private Stack<ICommand> redoStack = new Stack<ICommand>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        // Ctrlキー (MacではCmdキー) を検出
        bool isCtrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool isCmdDown = Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);

        if (isCtrlDown || isCmdDown)
        {
            // ZキーでUndo
            if (Input.GetKeyDown(KeyCode.Z))
            {
                Undo();
            }
            // YキーでRedo
            else if (Input.GetKeyDown(KeyCode.Y))
            {
                Redo();
            }
        }
    }

    public void RegisterCommand(ICommand command)
    {
        command.Execute(); // まずコマンドを実行
        undoStack.Push(command); // Undo履歴に追加
        redoStack.Clear(); // 新しい操作をしたらRedo履歴はクリア
    }

    public void Undo()
    {
        if (undoStack.Count > 0)
        {
            ICommand command = undoStack.Pop();
            command.Undo();
            redoStack.Push(command);
        }
    }

    public void Redo()
    {
        if (redoStack.Count > 0)
        {
            ICommand command = redoStack.Pop();
            command.Execute();
            undoStack.Push(command);
        }
    }
}