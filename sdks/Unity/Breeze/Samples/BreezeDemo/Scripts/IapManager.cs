using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;

namespace BreezeSdk.BreezeDemo
{
    public class IapManager : MonoBehaviour
    {
        private StoreController storeController;
        private bool isInitialized;
        private bool isInitializing;

        /// <summary>Raised when a purchase is successfully confirmed.</summary>
        public event Action OnPurchaseSuccess;

        /// <summary>Raised when a purchase fails. Parameter is error message.</summary>
        public event Action<string> OnPurchaseFailed;

        /// <summary>Raised when IAP has finished initializing (Connect + FetchProducts).</summary>
        public event Action<bool> OnInitialized;

        public bool IsInitialized => isInitialized;

        void OnDestroy()
        {
            if (storeController == null) return;

            storeController.OnPurchasePending -= OnPurchasePending;
            storeController.OnPurchaseConfirmed -= OnPurchaseConfirmed;
            storeController.OnPurchaseFailed -= HandleStorePurchaseFailed;
            storeController.OnProductsFetched -= OnProductsFetched;
            storeController.OnProductsFetchFailed -= OnProductsFetchFailed;
        }

        /// <summary>
        /// Initialize IAP: connect to store and fetch product catalog. Call once at startup (e.g. from Start).
        /// </summary>
        public async Awaitable InitializeAsync(List<ProductDefinition> productDefinitions)
        {
            if (isInitialized || isInitializing)
                return;

            isInitializing = true;

            try
            {
                storeController = UnityIAPServices.StoreController();

                storeController.OnPurchasePending += OnPurchasePending;
                storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
                storeController.OnPurchaseFailed += HandleStorePurchaseFailed;
                storeController.OnProductsFetched += OnProductsFetched;
                storeController.OnProductsFetchFailed += OnProductsFetchFailed;

                await storeController.Connect();

                if (productDefinitions == null || productDefinitions.Count == 0)
                {
                    Debug.LogWarning("[IAPManager] No product IDs configured.");
                    OnInitialized?.Invoke(false);
                    return;
                }

                var tcs = new TaskCompletionSource<bool>();
                void OnFetched(List<Product> _) => tcs.TrySetResult(true);
                void OnFetchFailed(ProductFetchFailed _) => tcs.TrySetResult(false);

                storeController.OnProductsFetched += OnFetched;
                storeController.OnProductsFetchFailed += OnFetchFailed;
                storeController.FetchProducts(productDefinitions);

                await tcs.Task;

                storeController.OnProductsFetched -= OnFetched;
                storeController.OnProductsFetchFailed -= OnFetchFailed;

                isInitialized = true;
                OnInitialized?.Invoke(true);
                Debug.Log("[IAPManager] Initialized.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IAPManager] Initialize failed: {ex}");
                OnInitialized?.Invoke(false);
            }
            finally
            {
                isInitializing = false;
            }
        }

        /// <summary>
        /// Start a purchase for the given product ID. IAP must be initialized first (e.g. wait for OnInitialized or IsInitialized).
        /// </summary>
        public void PurchaseProduct(string productId)
        {
            if (storeController == null)
            {
                Debug.LogError("[IAPManager] Not initialized. Call InitializeAsync first.");
                OnPurchaseFailed?.Invoke("IAP not initialized.");
                return;
            }

            if (string.IsNullOrEmpty(productId))
            {
                Debug.LogError("[IAPManager] Product ID is null or empty.");
                OnPurchaseFailed?.Invoke("Invalid product ID.");
                return;
            }

            storeController.PurchaseProduct(productId);
        }

        void OnPurchasePending(PendingOrder pendingOrder)
        {
            storeController.ConfirmPurchase(pendingOrder);
        }

        void OnPurchaseConfirmed(Order order)
        {
            OnPurchaseSuccess?.Invoke();
        }

        void HandleStorePurchaseFailed(FailedOrder failedOrder)
        {
            var message = failedOrder == null ? "Purchase failed." : failedOrder.FailureReason.ToString();
            Debug.LogWarning($"[IAPManager] Purchase failed: {message}");
            OnPurchaseFailed?.Invoke(message);
        }

        void OnProductsFetched(List<Product> products)
        {
            // Handled in InitializeAsync via local handler
        }

        void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            // Handled in InitializeAsync via local handler
        }
    }
}