using System.Text.RegularExpressions;

namespace ElementalBlacksmithStory.Data
{
    /// <summary>
    /// 한국어 받침(종성) 유무를 판별하여 'A/B' 형태의 임의의 조사를 자동 선택해 주는 범용 유틸리티
    /// (은/는, 이/가, 을/를, 과/와, 이랑/랑, 이며/며, 으로/로 등 모든 조사 자동 대응)
    /// </summary>
    public static class KoreanJosaHelper
    {
        /// <summary>
        /// 따옴표나 특수문자를 제외하고 단어 끝에서 가장 가까운 유효 문자(한글/숫자/영문)를 추출
        /// </summary>
        public static char GetLastValidChar(string word)
        {
            if (string.IsNullOrEmpty(word)) return '\0';

            for (int i = word.Length - 1; i >= 0; i--)
            {
                char c = word[i];
                if (char.IsLetterOrDigit(c))
                {
                    return c;
                }
            }

            return word[^1];
        }

        /// <summary>
        /// 문자의 한글 종성(받침) 여부 판별 (숫자 및 영문 끝소리 발음 포함)
        /// </summary>
        public static bool HasJongseong(char c)
        {
            // 1. 한글 음절 (가 ~ 힣)
            if (c >= 0xAC00 && c <= 0xD7A3)
            {
                return (c - 0xAC00) % 28 > 0;
            }

            // 2. 숫자 발음 끝소리: 0(영-ㅇ), 1(일-ㄹ), 3(삼-ㅁ), 6(육-ㄱ), 7(칠-ㄹ), 8(팔-ㄹ)
            if (char.IsDigit(c))
            {
                return c is '0' or '1' or '3' or '6' or '7' or '8';
            }

            // 3. 영문 끝소리 (L, M, N 등 자음 받침 발음)
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
            {
                char lower = char.ToLowerInvariant(c);
                return lower is 'l' or 'm' or 'n';
            }

            return false;
        }

        /// <summary>
        /// 문자의 한글 종성이 'ㄹ'인지 판별 ('으로/로' 구분용)
        /// </summary>
        public static bool HasRieulJongseong(char c)
        {
            if (c >= 0xAC00 && c <= 0xD7A3)
            {
                return (c - 0xAC00) % 28 == 8; // 8 = 'ㄹ' 받침
            }

            // 숫자: 1(일), 7(칠), 8(팔)
            if (c is '1' or '7' or '8')
            {
                return true;
            }

            // 영문: L
            if (char.ToLowerInvariant(c) == 'l')
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 단어 뒤에 적합한 조사를 반환 (A/B 형태면 어떤 조사든 자동 분기)
        /// 예: AttachJosa("철검", "이랑/랑") -> "이랑"
        /// </summary>
        public static string AttachJosa(string word, string josaType)
        {
            if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(josaType)) return string.Empty;

            if (josaType.Contains('/'))
            {
                var parts = josaType.Split('/');
                if (parts.Length == 2)
                {
                    char lastChar = GetLastValidChar(word);
                    bool hasJong = HasJongseong(lastChar);
                    bool isRieul = HasRieulJongseong(lastChar);

                    // 유일한 국립국어원 예외: '으로/로' 계열 ('ㄹ' 받침이거나 받침이 없으면 '로')
                    if (parts[0].EndsWith("으로") && parts[1].EndsWith("로"))
                    {
                        return (hasJong && !isRieul) ? parts[0] : parts[1];
                    }

                    // 그 외 모든 일반 조사: 받침 있으면 앞(A), 없으면 뒤(B)
                    return hasJong ? parts[0] : parts[1];
                }
            }

            return josaType;
        }

        /// <summary>
        /// 문장 전체에서 "단어(A/B)" 패턴을 올바른 조사로 자동 치환
        /// 따옴표 감싸기("'단어'(A/B)" or "단어(A/B)") 모두 완벽 지원
        /// </summary>
        public static string ResolveJosa(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // ([^\s()]+) 뒤에 괄호 (A/B)가 붙은 패턴을 모두 매칭
            return Regex.Replace(text, @"([^\s()]+)\(([^()/\s]+/[^()/\s]+)\)", m =>
            {
                string wordWithQuotes = m.Groups[1].Value;
                string josaType = m.Groups[2].Value;

                return wordWithQuotes + AttachJosa(wordWithQuotes, josaType);
            });
        }
    }
}
