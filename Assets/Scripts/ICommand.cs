using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICommand
{
    void Execute(); // コマンドを実行する
    void Undo();    // 実行したコマンドを元に戻す
}
