using System.Text.RegularExpressions;

namespace Othello.Core.Model {
    // 플레이어 ID 규칙과 대전 설정 조합 검사 (설계서 2.4, 운영 세부 기준)
    // - ID는 1~12자 한글·영문·숫자만. #은 CPU 예약 ID 전용

    public static class PlayerIdRule {
        public const int MaxLength = 12;

        public const string Guide = "ID는 1~12자 한글·영문·숫자만 쓸 수 있습니다.";

        private static readonly Regex Pattern = new("^[가-힣A-Za-z0-9]{1,12}$", RegexOptions.Compiled);

        public static bool IsValid(string? id)
            => id != null && Pattern.IsMatch(id);

        // 시작할 수 없는 이유 (문제 없으면 null)
        public static string? GetSetupError(PlayerSlot black, PlayerSlot white) {
            if (black.IsCpu && white.IsCpu)
                return "CPU끼리는 대전할 수 없습니다.";

            if (!black.IsCpu && !IsValid(black.Id))
                return "1P(흑): " + Guide;

            if (!white.IsCpu && !IsValid(white.Id))
                return "2P(백): " + Guide;

            if (!black.IsCpu && !white.IsCpu && black.Id == white.Id)
                return "두 플레이어의 ID가 같습니다.";

            return null;
        }
    }
}
