using System;
using System.Collections.Generic;
using UnityEngine;

namespace IntanIsle.Core
{
    // ── Data structs ────────────────────────────────────────────────────

    /// <summary>Result of processing a full zero-waste rabbit harvest chain.</summary>
    [Serializable]
    public struct HarvestResult
    {
        /// <summary>Meat yield in kilograms.</summary>
        public float Meat;

        /// <summary>Organ yield in kilograms.</summary>
        public float Organs;

        /// <summary>Bone yield in kilograms.</summary>
        public float Bone;

        /// <summary>Fur quality rating (0–1).</summary>
        public float Fur;

        /// <summary>Fertiliser produced in kilograms.</summary>
        public float Fertiliser;

        /// <summary>Jerky units produced.</summary>
        public float Jerky;

        /// <summary>Dendeng units produced.</summary>
        public float Dendeng;

        /// <summary>Broth units produced.</summary>
        public float Broth;

        /// <summary>Sausage units produced.</summary>
        public float Sausage;
    }

    // ── Event structs ───────────────────────────────────────────────────

    /// <summary>Published to add an item to the player's inventory.</summary>
    public struct InventoryAddEvent
    {
        /// <summary>Item type identifier.</summary>
        public string ItemType;

        /// <summary>Quantity to add.</summary>
        public float Quantity;
    }

    /// <summary>Published when the player has surplus eligible for Pay It Forward.</summary>
    public struct PayItForwardEligibleEvent
    {
        /// <summary>Identifier of the rabbit that produced the surplus.</summary>
        public string RabbitId;

        /// <summary>Percentage of surplus over baseline needs (0–1).</summary>
        public float SurplusPercent;
    }

    /// <summary>Published when a harvest product is processed.</summary>
    public struct HarvestProductProcessedEvent
    {
        /// <summary>Identifier of the source rabbit.</summary>
        public string RabbitId;

        /// <summary>Product type identifier.</summary>
        public string ProductType;

        /// <summary>Quantity produced.</summary>
        public float Quantity;
    }

    // ── Telemetry constants ─────────────────────────────────────────────

    /// <summary>Telemetry event type constants for the harvest chain.</summary>
    public static class HarvestTelemetryEvents
    {
        public const string HarvestProcessed = "HARVEST_PROCESSED";
        public const string HarvestProductAdded = "HARVEST_PRODUCT_ADDED";
        public const string PayItForwardEligible = "PAY_IT_FORWARD_ELIGIBLE";
        public const string ZeroWasteScoreUpdated = "ZERO_WASTE_SCORE_UPDATED";
    }

    // ── Manager ─────────────────────────────────────────────────────────

    /// <summary>
    /// Singleton that processes the zero-waste rabbit harvest chain.
    /// Calculates all 9 products from breed data, adds them to inventory,
    /// tracks zero-waste utilisation, and checks Pay It Forward eligibility.
    /// </summary>
    public class HarvestChainManager : MonoBehaviour
    {
        private const int TotalProducts = 9;
        private const float PayItForwardThreshold = 0.20f;

        /// <summary>Singleton instance, persistent across scenes.</summary>
        public static HarvestChainManager Instance { get; private set; }

        [Header("Yield Ratios (fraction of meatYieldKg)")]
        [SerializeField] private float organRatio = 0.12f;
        [SerializeField] private float boneRatio = 0.08f;
        [SerializeField] private float fertiliserRatio = 0.10f;

        [Header("Processed Product Ratios (fraction of meat)")]
        [SerializeField] private float jerkyRatio = 0.25f;
        [SerializeField] private float dendengRatio = 0.20f;
        [SerializeField] private float brothRatio = 0.15f;
        [SerializeField] private float sausageRatio = 0.18f;

        [Header("Surplus")]
        [SerializeField] private float payItForwardThreshold = PayItForwardThreshold;

        [Header("Breed Data")]
        [SerializeField] private List<RabbitScriptableObject> breedDatabase;

        private float _lifetimeZeroWasteSum;
        private int _lifetimeHarvestCount;

        // ── Public properties ───────────────────────────────────────────

        /// <summary>
        /// Running average of zero-waste utilisation across all harvests (0–1).
        /// 1.0 means every harvest used all 9 product streams.
        /// </summary>
        public float ZeroWasteScore => _lifetimeHarvestCount > 0
            ? _lifetimeZeroWasteSum / _lifetimeHarvestCount
            : 0f;

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

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Processes a full zero-waste harvest chain for the given rabbit.
        /// Calculates all 9 products from breed data, adds them to inventory,
        /// and fires telemetry for each product.
        /// </summary>
        /// <param name="rabbitId">Identifier of the rabbit to harvest.</param>
        /// <returns>A <see cref="HarvestResult"/> containing all product yields.</returns>
        public HarvestResult ProcessHarvest(string rabbitId)
        {
            RabbitCareManager care = RabbitCareManager.Instance;
            RabbitScriptableObject breedData = null;

            // Resolve breed data
            if (care != null)
            {
                RabbitHealthState state = care.GetHealthState(rabbitId);
                // Look up breed from care manager's data is not directly exposed,
                // so we iterate breed database to find a match via telemetry context.
                // For now, use the first breed as fallback; callers should ensure
                // breed is resolvable.
            }

            breedData = ResolveBreedData(rabbitId);

            float meatKg = breedData != null ? breedData.MeatYieldKg : 1f;
            float furQual = breedData != null ? breedData.FurQuality : 0.5f;

            HarvestResult result = new HarvestResult
            {
                Meat = meatKg,
                Organs = meatKg * organRatio,
                Bone = meatKg * boneRatio,
                Fur = furQual,
                Fertiliser = meatKg * fertiliserRatio,
                Jerky = meatKg * jerkyRatio,
                Dendeng = meatKg * dendengRatio,
                Broth = meatKg * brothRatio,
                Sausage = meatKg * sausageRatio
            };

            // Add all products to inventory
            int productsUsed = 0;
            productsUsed += AddProduct(rabbitId, ProductTypes.Meat, result.Meat);
            productsUsed += AddProduct(rabbitId, ProductTypes.Organs, result.Organs);
            productsUsed += AddProduct(rabbitId, ProductTypes.Bone, result.Bone);
            productsUsed += AddProduct(rabbitId, ProductTypes.Fur, result.Fur);
            productsUsed += AddProduct(rabbitId, ProductTypes.Fertiliser, result.Fertiliser);
            productsUsed += AddProduct(rabbitId, ProductTypes.Jerky, result.Jerky);
            productsUsed += AddProduct(rabbitId, ProductTypes.Dendeng, result.Dendeng);
            productsUsed += AddProduct(rabbitId, ProductTypes.Broth, result.Broth);
            productsUsed += AddProduct(rabbitId, ProductTypes.Sausage, result.Sausage);

            // Zero-waste score for this harvest
            float wasteScore = (float)productsUsed / TotalProducts;
            _lifetimeZeroWasteSum += wasteScore;
            _lifetimeHarvestCount++;

            PublishTelemetry(HarvestTelemetryEvents.HarvestProcessed,
                $"{{\"rabbitId\":\"{rabbitId}\",\"meatKg\":{result.Meat:F4},\"wasteScore\":{wasteScore:F4}}}");

            PublishTelemetry(HarvestTelemetryEvents.ZeroWasteScoreUpdated,
                $"{{\"lifetimeScore\":{ZeroWasteScore:F4},\"harvestCount\":{_lifetimeHarvestCount}}}");

            // Pay It Forward eligibility
            CheckPayItForward(rabbitId, result);

            return result;
        }

        // ── Internals ───────────────────────────────────────────────────

        private RabbitScriptableObject ResolveBreedData(string rabbitId)
        {
            if (breedDatabase == null || breedDatabase.Count == 0) return null;

            // If RabbitCareManager exposes breed info in the future, match here.
            // For now return the first entry as a safe fallback; production code
            // should pass breed explicitly or extend RabbitCareManager's API.
            return breedDatabase.Count > 0 ? breedDatabase[0] : null;
        }

        private int AddProduct(string rabbitId, string productType, float quantity)
        {
            if (quantity <= 0f) return 0;

            PublishViaBus(new InventoryAddEvent
            {
                ItemType = productType,
                Quantity = quantity
            });

            PublishViaBus(new HarvestProductProcessedEvent
            {
                RabbitId = rabbitId,
                ProductType = productType,
                Quantity = quantity
            });

            PublishTelemetry(HarvestTelemetryEvents.HarvestProductAdded,
                $"{{\"rabbitId\":\"{rabbitId}\",\"product\":\"{productType}\",\"qty\":{quantity:F4}}}");

            return 1;
        }

        private void CheckPayItForward(string rabbitId, HarvestResult result)
        {
            // Total usable yield as a proxy for surplus calculation
            float totalYield = result.Meat + result.Organs + result.Bone
                + result.Fertiliser + result.Jerky + result.Dendeng
                + result.Broth + result.Sausage;

            // Baseline need: meat + one processed product
            float baselineNeed = result.Meat + result.Jerky;

            if (baselineNeed <= 0f) return;

            float surplus = (totalYield - baselineNeed) / baselineNeed;

            if (surplus >= payItForwardThreshold)
            {
                PublishViaBus(new PayItForwardEligibleEvent
                {
                    RabbitId = rabbitId,
                    SurplusPercent = surplus
                });

                PublishTelemetry(HarvestTelemetryEvents.PayItForwardEligible,
                    $"{{\"rabbitId\":\"{rabbitId}\",\"surplusPercent\":{surplus:F4}}}");
            }
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

        /// <summary>Canonical product type identifiers for the harvest chain.</summary>
        private static class ProductTypes
        {
            public const string Meat = "meat";
            public const string Organs = "organs";
            public const string Bone = "bone";
            public const string Fur = "fur";
            public const string Fertiliser = "fertiliser";
            public const string Jerky = "jerky";
            public const string Dendeng = "dendeng";
            public const string Broth = "broth";
            public const string Sausage = "sausage";
        }
    }
}