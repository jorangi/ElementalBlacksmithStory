using System.Collections.Generic;
using System.Text.RegularExpressions;
using ElementalBlacksmithStory.Core;
using UnityEngine;

namespace ElementalBlacksmithStory.Data
{
    [System.Serializable]
    public struct DialogueLine
    {
        public uint speakerId;
        [TextArea(2, 5)]
        public string text;

        /// <summary>
        /// 대사 내 삼항 연산 조건문({cond ? true : false}), {key} 플레이스홀더, 한국어 조사(A/B)를 연쇄적으로 처리하여 반환합니다.
        /// </summary>
        public string GetFormattedText(IReadOnlyDictionary<string, object> parameters)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (parameters == null || parameters.Count == 0) return KoreanJosaHelper.ResolveJosa(text);

            // 1단계: 삼항 조건문({ itemCount == 1 ? "참" : "거짓" }) 평가 및 치환
            string step1 = EvaluateTernaryConditions(text, parameters);

            // 2단계: {key:A/B} 또는 {key} 패턴 정규식 매칭 및 치환
            string step2 = Regex.Replace(step1, @"\{([a-zA-Z0-9_\.\[\]]+)(?::([^}]+/[^}]+))?\}", m =>
            {
                string key = m.Groups[1].Value;
                if (!parameters.TryGetValue(key, out var val) || val == null)
                {
                    return m.Value; // 파라미터가 없으면 원래 플레이스홀더 유지
                }

                string valStr = val.ToString();
                if (m.Groups[2].Success)
                {
                    string josa = m.Groups[2].Value;
                    return valStr + KoreanJosaHelper.AttachJosa(valStr, josa);
                }

                return valStr;
            });

            // 3단계: 단어 뒤에 붙은 (A/B) 괄호 형태의 한국어 조사 최종 보정
            return KoreanJosaHelper.ResolveJosa(step2);
        }

        private static string EvaluateTernaryConditions(string rawText, IReadOnlyDictionary<string, object> parameters)
        {
            if (string.IsNullOrEmpty(rawText) || !rawText.Contains('?') || !rawText.Contains(':'))
            {
                return rawText;
            }

            var sb = new System.Text.StringBuilder();
            int i = 0;

            while (i < rawText.Length)
            {
                if (rawText[i] == '{')
                {
                    int start = i;
                    int depth = 0;
                    int questionIdx = -1;
                    int colonIdx = -1;

                    for (int j = i; j < rawText.Length; j++)
                    {
                        char c = rawText[j];
                        if (c == '{')
                        {
                            depth++;
                        }
                        else if (c == '}')
                        {
                            depth--;
                            if (depth == 0)
                            {
                                // 삼항 연산자({ cond ? true : false })인지 판별
                                if (questionIdx != -1 && colonIdx != -1 && questionIdx < colonIdx)
                                {
                                    string conditionStr = rawText.Substring(start + 1, questionIdx - (start + 1)).Trim();
                                    string trueText = TrimQuotes(rawText.Substring(questionIdx + 1, colonIdx - (questionIdx + 1)).Trim());
                                    string falseText = TrimQuotes(rawText.Substring(colonIdx + 1, j - (colonIdx + 1)).Trim());

                                    bool isTrue = EvaluateCondition(conditionStr, parameters);
                                    string selected = isTrue ? trueText : falseText;
                                    if (selected.Contains('?') && selected.Contains(':'))
                                    {
                                        selected = EvaluateTernaryConditions(selected, parameters);
                                    }
                                    sb.Append(selected);
                                    i = j + 1;
                                    goto NextChar;
                                }
                                else
                                {
                                    // 일반 플레이스홀더(예: {item1})인 경우 그대로 유지
                                    sb.Append(rawText.Substring(start, j - start + 1));
                                    i = j + 1;
                                    goto NextChar;
                                }
                            }
                        }
                        else if (depth == 1)
                        {
                            // 조건문 최상위 레벨에서의 ? 와 : 만 캡처 (내부 {변수} 속 ?/: 무시)
                            if (c == '?' && questionIdx == -1)
                            {
                                questionIdx = j;
                            }
                            else if (c == ':' && questionIdx != -1 && colonIdx == -1)
                            {
                                colonIdx = j;
                            }
                        }
                    }

                    sb.Append(rawText[i]);
                    i++;
                }
                else
                {
                    sb.Append(rawText[i]);
                    i++;
                }

            NextChar:;
            }

            return sb.ToString();
        }

        private static string TrimQuotes(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if ((s.StartsWith('"') && s.EndsWith('"')) || (s.StartsWith('\'') && s.EndsWith('\'')))
            {
                if (s.Length >= 2) return s.Substring(1, s.Length - 2);
            }
            return s;
        }

        private static bool EvaluateCondition(string condition, IReadOnlyDictionary<string, object> parameters)
        {
            // 1. 2글자 및 1글자 비교 연산자 파싱
            string[] ops = { "==", "!=", ">=", "<=", ">", "<" };
            string matchedOp = null;
            int opIndex = -1;

            foreach (var op in ops)
            {
                int idx = condition.IndexOf(op, System.StringComparison.Ordinal);
                if (idx >= 0)
                {
                    matchedOp = op;
                    opIndex = idx;
                    break;
                }
            }

            if (matchedOp != null)
            {
                string leftKey = condition.Substring(0, opIndex).Trim();
                string rightLiteral = TrimQuotes(condition.Substring(opIndex + matchedOp.Length).Trim());

                parameters.TryGetValue(leftKey, out var leftVal);
                string leftStr = leftVal?.ToString() ?? string.Empty;

                // 숫자 비교
                if (double.TryParse(leftStr, out double leftNum) && double.TryParse(rightLiteral, out double rightNum))
                {
                    return matchedOp switch
                    {
                        "==" => System.Math.Abs(leftNum - rightNum) < 0.000001,
                        "!=" => System.Math.Abs(leftNum - rightNum) >= 0.000001,
                        ">" => leftNum > rightNum,
                        "<" => leftNum < rightNum,
                        ">=" => leftNum >= rightNum,
                        "<=" => leftNum <= rightNum,
                        _ => false
                    };
                }

                // 불리언 비교
                if (bool.TryParse(leftStr, out bool leftBool) && bool.TryParse(rightLiteral, out bool rightBool))
                {
                    return matchedOp switch
                    {
                        "==" => leftBool == rightBool,
                        "!=" => leftBool != rightBool,
                        _ => false
                    };
                }

                // 문자열 비교
                return matchedOp switch
                {
                    "==" => string.Equals(leftStr, rightLiteral, System.StringComparison.OrdinalIgnoreCase),
                    "!=" => !string.Equals(leftStr, rightLiteral, System.StringComparison.OrdinalIgnoreCase),
                    _ => false
                };
            }

            // 2. 연산자가 없는 단일 불리언 또는 플래그 { isVIP ? ... : ... }
            if (parameters.TryGetValue(condition, out var singleVal) && singleVal != null)
            {
                if (singleVal is bool b) return b;
                if (double.TryParse(singleVal.ToString(), out double n)) return n != 0;
                return !string.IsNullOrEmpty(singleVal.ToString()) && !string.Equals(singleVal.ToString(), "false", System.StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        /// <summary>
        /// 간편 호출용: line.GetFormattedText(("username", "조랑이"), ("gold", 1500))
        /// </summary>
        public string GetFormattedText(params (string key, object val)[] parameters)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (parameters == null || parameters.Length == 0) return KoreanJosaHelper.ResolveJosa(text);

            var dict = new Dictionary<string, object>(parameters.Length);
            for (int i = 0; i < parameters.Length; i++)
            {
                dict[parameters[i].key] = parameters[i].val;
            }

            return GetFormattedText(dict);
        }
    }

    /// <summary>
    /// NPC 대화 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "SO DialogueData", menuName = "Scriptable Objects/SO_DialogueData")]
    public class SO_DialogueData : ScriptableObject, IIdentifiable
    {
        public uint Id => uint.TryParse(name, out var id) ? id : 0;
        [TextArea(2, 5)]
        public string description;
        public List<DialogueLine> contents = new();
    }
}