using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace IntanIsle.Core
{
    // ── Inner data types ────────────────────────────────────────────────

    /// <summary>A single reflection journal entry.</summary>
    [Serializable]
    public class ReflectionEntry
    {
        /// <summary>Unique entry identifier (GUID).</summary>
        public string entryId;

        /// <summary>The prompt that was presented to the player.</summary>
        public string prompt;

        /// <summary>The player's written response.</summary>
        public string response;

        /// <summary>Word count of the response.</summary>
        public int wordCount;

        /// <summary>Pillar identifiers this reflection relates to.</summary>
        public string[] pillarIds;

        /// <summary>Mood tag chosen by the player.</summary>
        public string moodTag;

        /// <summary>Unix timestamp in milliseconds when submitted.</summary>
        public long submittedAtMs;

        /// <summary>Blessing event identifier tied to this entry.</summary>
        public string blessingEventId;
    }

    /// <summary>Valid mood tag identifiers for reflection entries.</summary>
    public static class MoodTags
    {
        public const string Calm = "calm";
        public const string Curious = "curious";
        public const string Proud = "proud";
        public const string Worried = "worried";
        public const string Grateful = "grateful";
        public const string Uncertain = "uncertain";
    }

    /// <summary>Sentiment analysis result stub. Production: computed by NurAIN Lite.</summary>
    public enum SentimentFlag
    {
        Neutral,
        Positive,
        Reflective,
        Distressed,
        Unanalysed
    }

    /// <summary>Published when a reflection is submitted with sentiment analysis.</summary>
    public struct ReflectionAnalysedEvent
    {
        public string EntryId;
        public int WordCount;
        public string MoodTag;
        public SentimentFlag Sentiment;
        public string[] PillarIds;
    }

    // ── Serialisation wrapper ───────────────────────────────────────────

    [Serializable]
    internal class ReflectionSaveData
    {
        public List<ReflectionEntry> entries = new List<ReflectionEntry>();
    }

    // ── Telemetry constants ─────────────────────────────────────────────

    /// <summary>Telemetry event type constants for the reflection journal.</summary>
    public static class ReflectionTelemetryEvents
    {
        public const string ReflectionSubmitted = "REFLECTION_SUBMITTED";
    }

    // ── Manager ─────────────────────────────────────────────────────────

    /// <summary>
    /// Singleton that manages the in-game reflection journal.
    /// Validates responses by age-group word count, persists entries
    /// to a local JSON file, and awards blessing on submission.
    /// </summary>
    public class ReflectionJournalManager : MonoBehaviour
    {
        private const int MaxEntries = 500;
        private const string FilePrefix = "reflections_";
        private const string FileExtension = ".json";
        private const float SubmissionBlessingDelta = 1f;

        private const int MinWordsNursery = 15;
        private const int MinWordsPrimary = 30;
        private const int MinWordsTeen = 50;

        /// <summary>Singleton instance, persistent across scenes.</summary>
        public static ReflectionJournalManager Instance { get; private set; }

        [SerializeField] private int maxEntries = MaxEntries;

        private readonly List<ReflectionEntry> _entries = new List<ReflectionEntry>();

        // ── Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            LoadEntries();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                SaveEntries();
        }

        private void OnApplicationQuit()
        {
            SaveEntries();
        }

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Submits a new reflection entry. Validates that the response meets
        /// the minimum word count for the player's age group. Runs a local
        /// sentiment analysis stub and fires curriculum pillar tags.
        /// </summary>
        /// <param name="prompt">The reflection prompt shown to the player.</param>
        /// <param name="response">The player's written response.</param>
        /// <param name="pillarIds">Pillar identifiers this reflection relates to.</param>
        /// <param name="moodTag">Optional mood tag (calm, curious, proud, worried, grateful, uncertain).</param>
        /// <returns>True if the reflection was accepted and saved.</returns>
        public bool SubmitReflection(string prompt, string response, string[] pillarIds, string moodTag = "")
        {
            if (string.IsNullOrEmpty(response)) return false;

            int wordCount = CountWords(response);
            int minWords = GetMinWordCount();

            if (wordCount < minWords) return false;

            string entryId = Guid.NewGuid().ToString();
            string blessingEventId = Guid.NewGuid().ToString();

            // Sentiment analysis stub — local heuristic, production uses NurAIN Lite
            SentimentFlag sentiment = AnalyseSentimentStub(response, moodTag);

            ReflectionEntry entry = new ReflectionEntry
            {
                entryId = entryId,
                prompt = prompt ?? string.Empty,
                response = response,
                wordCount = wordCount,
                pillarIds = pillarIds ?? Array.Empty<string>(),
                moodTag = string.IsNullOrEmpty(moodTag) ? string.Empty : moodTag,
                submittedAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                blessingEventId = blessingEventId
            };

            _entries.Add(entry);
            PruneIfNeeded();
            SaveEntries();

            // Telemetry
            PublishTelemetry(ReflectionTelemetryEvents.ReflectionSubmitted,
                $"{{\"entryId\":\"{entryId}\",\"wordCount\":{wordCount},\"mood\":\"{entry.moodTag}\",\"sentiment\":\"{sentiment}\"}}");

            // Blessing: +5 base (spec Section 6)
            PublishViaBus(new BlessingDeltaRequest
            {
                Delta = 5f,
                Reason = "Reflection journal entry submitted"
            });

            // Publish analysis event for dashboard/NurAIN
            PublishViaBus(new ReflectionAnalysedEvent
            {
                EntryId = entryId,
                WordCount = wordCount,
                MoodTag = entry.moodTag,
                Sentiment = sentiment,
                PillarIds = entry.pillarIds
            });

            // Tag curriculum pillars (Reflection + Written Expression)
            CurriculumEngine engine = CurriculumEngine.Instance;
            if (engine != null && entry.pillarIds != null)
            {
                foreach (string pillar in entry.pillarIds)
                    engine.RecordActivity(pillar, 2f); // depth 2 = engagement (made a choice)
            }

            return true;
        }

        /// <summary>
        /// Stub sentiment analysis. Production: NurAIN Lite processes the text
        /// server-side and returns a SentimentFlag. This local heuristic uses
        /// mood tag and word count as proxy signals.
        /// </summary>
        private static SentimentFlag AnalyseSentimentStub(string response, string moodTag)
        {
            // Stub: derive from mood tag if provided
            if (!string.IsNullOrEmpty(moodTag))
            {
                switch (moodTag)
                {
                    case MoodTags.Calm:
                    case MoodTags.Grateful:
                    case MoodTags.Proud:
                        return SentimentFlag.Positive;

                    case MoodTags.Curious:
                    case MoodTags.Uncertain:
                        return SentimentFlag.Reflective;

                    case MoodTags.Worried:
                        return SentimentFlag.Distressed;
                }
            }

            // Fallback: longer responses = more reflective
            int words = CountWords(response);
            if (words > 80) return SentimentFlag.Reflective;
            if (words > 40) return SentimentFlag.Positive;

            return SentimentFlag.Neutral;
        }

        /// <summary>
        /// Returns the minimum word count required for the current player's age group.
        /// Nursery/7–9: 15, Primary/10–12: 30, Teen/13+: 50.
        /// </summary>
        /// <returns>Minimum word count.</returns>
        public int GetMinWordCount()
        {
            PlayerDataManager player = PlayerDataManager.Instance;
            if (player == null) return MinWordsNursery;

            switch (player.PlayerAgeGroup)
            {
                case AgeGroup.Nursery_7_9: return MinWordsNursery;
                case AgeGroup.Primary_10_12: return MinWordsPrimary;
                case AgeGroup.Teen_13_Plus: return MinWordsTeen;
                default: return MinWordsNursery;
            }
        }

        /// <summary>
        /// Returns a read-only view of all stored reflection entries.
        /// </summary>
        /// <returns>Read-only list of entries, newest last.</returns>
        public IReadOnlyList<ReflectionEntry> GetAllEntries()
        {
            return _entries;
        }

        /// <summary>
        /// Sets the mood tag on the most recent entry.
        /// Must be one of: calm, curious, proud, worried, grateful, uncertain.
        /// </summary>
        /// <param name="entryId">Entry identifier.</param>
        /// <param name="moodTag">Mood tag string.</param>
        public void SetMoodTag(string entryId, string moodTag)
        {
            if (string.IsNullOrEmpty(entryId) || string.IsNullOrEmpty(moodTag)) return;

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i].entryId == entryId)
                {
                    _entries[i].moodTag = moodTag;
                    return;
                }
            }
        }

        // ── Persistence ─────────────────────────────────────────────────

        private void SaveEntries()
        {
            ReflectionSaveData save = new ReflectionSaveData
            {
                entries = new List<ReflectionEntry>(_entries)
            };

            string json = JsonUtility.ToJson(save, true);

            try
            {
                File.WriteAllText(GetSavePath(), json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ReflectionJournalManager] Failed to save: {e.Message}");
            }
        }

        private void LoadEntries()
        {
            _entries.Clear();

            string path = GetSavePath();
            if (!File.Exists(path)) return;

            try
            {
                string json = File.ReadAllText(path);
                ReflectionSaveData save = JsonUtility.FromJson<ReflectionSaveData>(json);
                if (save != null && save.entries != null)
                    _entries.AddRange(save.entries);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ReflectionJournalManager] Failed to load: {e.Message}");
            }
        }

        private string GetSavePath()
        {
            PlayerDataManager player = PlayerDataManager.Instance;
            string playerId = player != null ? player.PlayerId : "unknown";
            return Path.Combine(Application.persistentDataPath,
                FilePrefix + playerId + FileExtension);
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private void PruneIfNeeded()
        {
            while (_entries.Count > maxEntries)
                _entries.RemoveAt(0);
        }

        private static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;

            int count = 0;
            bool inWord = false;

            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    inWord = false;
                }
                else if (!inWord)
                {
                    inWord = true;
                    count++;
                }
            }

            return count;
        }

        private void PublishViaBus<T>(T eventData)
        {
            EventBus bus = EventBus.Instance;
            if (bus != null)
                bus.Publish(eventData);
        }

        private void PublishTelemetry(string eventType, string jsonPayload)
        {
            PublishViaBus(new TelemetryRequestEvent
            {
                EventType = eventType,
                JsonPayload = jsonPayload
            });
        }
    }
}