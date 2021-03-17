

namespace IngameScript {
    /// <summary>
    /// yoinked from https://codereview.stackexchange.com/questions/146916/pronounceable-name-generator
    /// modified for use
    /// </summary>
    public class NameGen {
        char[] vowels = "aeiou".ToCharArray();
        char[] consonants = "bcdfghjklmnpqrstvwxyz".ToCharArray();
        public string Next(int halfLength = 3) {
            char[] result = new char[halfLength * 2];
            for (int i = 0; i < halfLength; i++) {
                result[i * 2] = vowels[MAF.random.Next(0, 5)];
                if (i == 0) {
                    result[i] -= (char)32;
                }
                result[i * 2 + 1] = consonants[MAF.random.Next(0, 21)];
            }

            return new string(result);
        }
    }
}