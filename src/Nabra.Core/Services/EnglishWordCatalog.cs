using System;
using System.Collections.Generic;

namespace Nabra.Core.Services
{
    /// <summary>
    /// A comprehensive catalog of common English words and linguistic heuristics used by the
    /// real-time conversion service when Auto-Detect is enabled, preventing legitimate English
    /// typing from being accidentally converted into Arabic gibberish.
    /// Pure local static set: zero network calls, zero disk reads.
    /// </summary>
    public static class EnglishWordCatalog
    {
        private static readonly HashSet<string> Words = new(StringComparer.OrdinalIgnoreCase)
        {
            // Pronouns, articles, conjunctions, prepositions
            "the", "be", "to", "of", "and", "a", "in", "that", "have", "i", "it", "for", "not", "on", "with",
            "he", "as", "you", "do", "at", "this", "but", "his", "by", "from", "they", "we", "say", "her",
            "she", "or", "an", "will", "my", "one", "all", "would", "there", "their", "what", "so", "up",
            "out", "if", "about", "who", "get", "which", "go", "me", "when", "make", "can", "like", "time",
            "no", "just", "him", "know", "take", "people", "into", "year", "your", "good", "some", "could",
            "them", "see", "other", "than", "then", "now", "look", "only", "come", "its", "over", "think",
            "also", "back", "after", "use", "two", "how", "our", "work", "first", "well", "way", "even",
            "new", "want", "because", "any", "these", "give", "day", "most", "us", "is", "are", "was", "were",
            "been", "has", "had", "did", "does", "done", "am",

            // Common verbs & auxiliaries
            "find", "tell", "ask", "seem", "feel", "try", "leave", "call", "keep", "let", "begin", "show",
            "hear", "run", "might", "play", "move", "live", "believe", "bring", "happen", "must", "write",
            "provide", "sit", "stand", "lose", "pay", "meet", "include", "continue", "set", "learn", "change",
            "lead", "understand", "watch", "follow", "stop", "create", "speak", "read", "allow", "add", "spend",
            "grow", "open", "walk", "win", "offer", "remember", "love", "consider", "appear", "buy", "wait",
            "serve", "send", "expect", "build", "stay", "fall", "cut", "reach", "kill", "remain", "suggest",
            "raise", "pass", "sell", "require", "report", "decide", "pull", "return", "explain", "hope", "develop",
            "carry", "break", "receive", "agree", "support", "hit", "produce", "eat", "cover", "catch", "draw",
            "choose", "cause", "point", "listen", "test", "check", "start", "help", "need", "mean", "talk",
            "turn", "put", "end", "close", "save", "load", "type", "press", "click", "hold", "drag", "drop",
            "enter", "select", "switch", "enable", "disable", "reset", "apply", "cancel", "verify", "finish",
            "sent", "wrote", "built", "went", "came", "made", "said", "took", "seen", "given", "known", "felt",

            // Common nouns & concepts
            "world", "life", "hand", "part", "child", "eye", "woman", "place", "case", "week", "company",
            "system", "program", "question", "work", "government", "number", "night", "point", "home", "water",
            "room", "mother", "area", "money", "story", "fact", "month", "lot", "right", "study", "book",
            "job", "word", "business", "issue", "side", "kind", "head", "house", "service", "friend", "father",
            "power", "hour", "game", "line", "end", "member", "law", "car", "city", "community", "name",
            "president", "team", "minute", "idea", "kid", "body", "information", "back", "parent", "face",
            "others", "level", "office", "door", "health", "person", "art", "war", "history", "party", "result",
            "change", "morning", "reason", "research", "girl", "guy", "moment", "air", "teacher", "force", "education",
            "foot", "boy", "age", "policy", "music", "market", "sense", "nation", "plan", "college", "interest",
            "death", "experience", "effect", "class", "control", "care", "field", "development", "role",
            "effort", "rate", "heart", "drug", "leader", "light", "voice", "wife", "police", "mind",
            "price", "view", "source", "title", "text", "file", "folder", "code", "image", "data", "page",
            "screen", "key", "layout", "button", "window", "mouse", "keyboard", "menu", "tab", "space",

            // Computing, tech & internet
            "computer", "laptop", "desktop", "phone", "mobile", "tablet", "device", "server", "client", "network",
            "internet", "web", "website", "site", "online", "offline", "browser", "chrome", "edge", "firefox",
            "safari", "google", "windows", "microsoft", "apple", "linux", "android", "github", "gitlab",
            "chat", "message", "email", "mail", "post", "comment", "profile", "account", "user", "username",
            "password", "passcode", "security", "privacy", "setting", "settings", "option", "options", "config",
            "project", "version", "update", "upgrade", "download", "upload", "install", "status", "ready",
            "error", "warning", "info", "log", "logs", "link", "url", "tool", "tools", "icon", "dark", "light",
            "auto", "manual", "custom", "default", "mode", "theme", "color", "accent", "scale", "speed",
            "document", "doc", "docx", "pdf", "txt", "zip", "rar", "folder", "path", "drive", "disk", "ram",
            "cpu", "gpu", "usb", "audio", "video", "media", "camera", "mic", "sound", "volume", "display",
            "monitor", "setup", "driver", "task", "process", "thread", "memory", "storage", "backup", "restore",

            // Common adjectives & adverbs
            "great", "small", "large", "big", "high", "different", "next", "early", "young",
            "important", "few", "public", "bad", "same", "able", "far", "long", "little", "own",
            "old", "full", "easy", "hard", "clear", "recent", "certain", "personal", "open", "red",
            "blue", "green", "black", "white", "dark", "fast", "slow", "simple", "real", "best",
            "better", "true", "free", "short", "strong", "special", "late", "general", "specific",
            "main", "major", "happy", "fine", "ready", "direct", "quick", "sure", "very", "too",
            "more", "much", "never", "always", "often", "sometimes", "already", "together", "almost",
            "enough", "really", "probably", "actually", "maybe", "especially", "finally", "simply", "easily",
            "today", "tomorrow", "yesterday", "tonight", "soon", "late", "later", "daily", "weekly",

            // Conversational & casual
            "hello", "hi", "hey", "yes", "yeah", "yep", "no", "nope", "ok", "okay", "thanks", "thank",
            "please", "welcome", "bye", "goodbye", "cool", "nice", "fine", "awesome", "perfect", "done",
            "sorry", "excuse", "pardon", "congrats", "cheers", "bro", "dude", "sir", "madam",

            // Numbers & days
            "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
            "first", "second", "third", "sunday", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday",
            "january", "february", "march", "april", "may", "june", "july", "august", "september", "october", "november", "december"
        };

        private static readonly char[] EnglishVowels = { 'a', 'e', 'i', 'o', 'u', 'y', 'A', 'E', 'I', 'O', 'U', 'Y' };
        private static readonly char[] ArabicPunctuationKeys = { ';', '\'', '[', ']', '`' };

        /// <summary>
        /// Checks if a string is a recognized common English word.
        /// </summary>
        public static bool IsCommonEnglishWord(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            var clean = word.Trim(' ', '\t', '.', ',', '!', '?', ';', ':', '-', '_', '\'', '"', '(', ')', '[', ']', '{', '}');
            return clean.Length > 0 && Words.Contains(clean);
        }

        /// <summary>
        /// Linguistic heuristic: returns true if the token looks like authentic English text,
        /// or false if it contains keyboard layout artifacts that identify it as mistyped Arabic.
        /// </summary>
        public static bool LooksLikeEnglishWord(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;

            // 1. If it contains punctuation keys that map to Arabic letters on English layout,
            // it CANNOT be an English word (e.g. "hofhv;", "a;vh", ";dt;", "]og", "[ldg")
            if (word.IndexOfAny(ArabicPunctuationKeys) >= 0)
                return false;

            // 2. Exact match in the English dictionary
            if (IsCommonEnglishWord(word))
                return true;

            // 3. Any English token of length >= 2 MUST contain at least one English vowel (a, e, i, o, u, y).
            // Arabic words mistyped on English layout are almost always vowel-less clusters (e.g. lvpfh, lh, sghl, jlhl, lk, td, hgsghl)
            if (word.Length >= 2 && word.IndexOfAny(EnglishVowels) < 0)
                return false;

            return false;
        }
    }
}
