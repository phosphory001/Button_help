using System;
using System.Collections.Generic;
using System.Text;

public class StringSplitter
{
  public static List<string> SplitStringWithTags(string input)
  {
    List<string> result = new List<string>();
    if (string.IsNullOrEmpty(input))
    {
      return result;
    }

    StringBuilder currentText = new StringBuilder();
    bool insideTag = false;

    for (int i = 0; i < input.Length; i++)
    {
      char c = input[i];

      if (c == '<')
      {
        // 如果遇到 '<'，并且不在标签内，则开始一个标签
        if (!insideTag)
        {
          // 如果当前有未处理的普通字符，先加入结果
          if (currentText.Length > 0)
          {
            foreach (char ch in currentText.ToString())
            {
              result.Add(ch.ToString());
            }
            currentText.Clear();
          }
          insideTag = true;
          currentText.Append(c);
        }
        else
        {
          // 如果已经在标签内，直接追加（可能是嵌套标签，这里暂不处理）
          currentText.Append(c);
        }
      }
      else if (c == '>')
      {
        // 如果遇到 '>' 并且在标签内，则结束标签
        if (insideTag)
        {
          currentText.Append(c);
          result.Add(currentText.ToString());
          currentText.Clear();
          insideTag = false;
        }
        else
        {
          // 如果不在标签内，当作普通字符处理
          currentText.Append(c);
        }
      }
      else
      {
        // 普通字符，直接追加
        currentText.Append(c);
      }
    }

    // 处理剩余的字符（如果有）
    if (currentText.Length > 0)
    {
      if (insideTag)
      {
        // 如果仍在标签内，把未闭合的标签加入结果
        result.Add(currentText.ToString());
      }
      else
      {
        // 否则，逐个字符加入
        foreach (char ch in currentText.ToString())
        {
          result.Add(ch.ToString());
        }
      }
    }

    return result;
  }
}