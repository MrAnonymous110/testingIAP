using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Purchasing;
using UnityEngine.UI;

/// <summary>
/// An example of basic Unity IAP functionality.
/// To use with your account, configure the product ids (AddProduct)
/// and Google Play key (SetPublicKey).
/// </summary>
[AddComponentMenu("Unity IAP/UnityIAPDemo")]
public class UnityIAPDemo : MonoBehaviour, IStoreListener
{
    // Unity IAP objects 
    private IStoreController m_Controller;
    private IAppleExtensions m_AppleExtensions;

    private int m_SelectedItemIndex = -1; // -1 == no product

    /// <summary>
    /// This will be called when Unity IAP has finished initialising.
    /// </summary>
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        m_Controller = controller;
        m_AppleExtensions = extensions.GetExtension<IAppleExtensions> ();

        Debug.Log("Available items:");
        foreach (var item in controller.products.all)
        {
            if (item.availableToPurchase)
            {
                Debug.Log(string.Join(" - ",
                    new[]
                    {
                        item.metadata.localizedTitle,
                        item.metadata.localizedDescription,
                        item.metadata.isoCurrencyCode,
                        item.metadata.localizedPrice.ToString(),
                        item.metadata.localizedPriceString
                    }));
            }
        }

        if (null != m_Controller)
        {
            // Prepare model for purchasing
            if (m_Controller.products.all.Length > 0) 
            {
                m_SelectedItemIndex = 0;
            }

            // Populate the product menu now that we have Products
            for (int t = 0; t < m_Controller.products.all.Length; t++)
            {
                var item = m_Controller.products.all[t];
                var description = string.Format("{0} - {1}", item.metadata.localizedTitle, item.metadata.localizedPriceString);

                // NOTE: my options list is created in InitUI
                GetDropdown().options[t] = new Dropdown.OptionData(description);
            }

            // Ensure I render the selected list element
            GetDropdown().RefreshShownValue();

            // Now that I have real products, begin showing product purchase history
            UpdateHistoryUI();
        }
    }

    /// <summary>
    /// This will be called when a purchase completes.
    /// </summary>
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
    {
		Debug.Log("Purchase OK: " + e.purchasedProduct.definition.id);
		Debug.Log("Receipt: " + e.purchasedProduct.receipt);

        // Now that my purchase history has changed, update its UI
        UpdateHistoryUI();

        // Indicate we have handled this purchase, we will not be informed of it again.
        return PurchaseProcessingResult.Complete;
    }

    /// <summary>
    /// This will be called is an attempted purchase fails.
    /// </summary>
    public void OnPurchaseFailed(Product item, PurchaseFailureReason r)
    {
        Debug.Log("Purchase failed: " + item.definition.id);
        Debug.Log(r);
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.Log("Billing failed to initialize!");
        switch (error)
        {
            case InitializationFailureReason.AppNotKnown:
                Debug.LogError("Is your App correctly uploaded on the relevant publisher console?");
                break;
            case InitializationFailureReason.PurchasingUnavailable:
                // Ask the user if billing is disabled in device settings.
                Debug.Log("Billing disabled!");
                break;
            case InitializationFailureReason.NoProductsAvailable:
                // Developer configuration error; check product metadata.
                Debug.Log("No products available for purchase!");
                break;
        }
    }

    public void Awake()
    {
        var module = StandardPurchasingModule.Instance();
        module.useMockBillingSystem = true;
        var builder = ConfigurationBuilder.Instance(module);

        builder.Configure<IGooglePlayConfiguration>().SetPublicKey("MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAoijNfTc9kS+M6R12DNc32dG7Mrg/czFABHPgYg8IQ8xebBoQMRXGEbmm4CHoNxCSJk+Fcs05wTogSsGbN0uemOmJUwCYowLQxzIeOBqH4aB2kDqMSDqKJnK08wDUFtDIrIBsczFincIW4i0E4JqtrGPksqn/tG5SvKvheG4x8yUcLWbd/MC885cZY1lMoRsaakVf/EoMfqtOccH+1x2dFVh0Q/NTAkgExILsN4KXHTpM2mHLKr6Dv3NK9ueYsLvf39kL9CwLZFFyD3413cyqtR4ZdfHL0BHw+9rSr0uysMtU3SGfqBHgyjMVNFL0eL6txWgVDhL3MaAXokIm0CePiwIDAQAB");
        // builder.AddProduct("coins", ProductType.Consumable, new IDs
        // {
        //     {"com.unity3d.unityiap.unityiapdemo.100goldcoins.v2.c", GooglePlay.Name},
        //     {"com.unity3d.unityiap.unityiapdemo.100goldcoins.6", AppleAppStore.Name},
        //     {"com.unity3d.unityiap.unityiapdemo.100goldcoins.mac", MacAppStore.Name},
        //     {"com.unity3d.unityiap.unityiapdemo.100goldcoins.win8", WinRT.Name}
        // });
        //
        // builder.AddProduct("sword", ProductType.NonConsumable, new IDs
        // {
        //     {"com.unity3d.unityiap.unityiapdemo.sword.c", GooglePlay.Name},
        //     {"com.unity3d.unityiap.unityiapdemo.sword.6", AppleAppStore.Name},
        //     {"com.unity3d.unityiap.unityiapdemo.sword.mac", MacAppStore.Name},
        //     {"com.unity3d.unityiap.unityiapdemo.sword", WindowsPhone8.Name}
        // });
        builder.AddProduct("sub1week", ProductType.Subscription, new IDs
        {
            {"sub1week", GooglePlay.Name},
        });
        
        builder.AddProduct("sub1month", ProductType.Subscription, new IDs
        {
            {"sub1month", GooglePlay.Name}
        });

        // First crack at UI configuration
        InitUI(builder.products);

        // Now we're ready to initialize Unity IAP.
        UnityPurchasing.Initialize(this, builder);
    }

    /// <summary>
    /// This will be called after a call to <extension>.RestoreTransactions().
    /// </summary>
    private void OnTransactionsRestored(bool success)
    {
        Debug.Log("Transactions restored.");
    }

    /// <summary>
    /// iOS Specific.
    /// This is called as part of Apple's 'Ask to buy' functionality,
    /// when a purchase is requested by a minor and referred to a parent
    /// for approval.
    /// 
    /// When the purchase is approved or rejected, the normal purchase events
    /// will fire.
    /// </summary>
    /// <param name="item">Item.</param>
    private void OnDeferred(Product item)
    {
        Debug.Log("Purchase deferred: " + item.definition.id);
    }

    private void InitUI(HashSet<ProductDefinition> items)
    {
        foreach (var item in items)
        {
            // Add initial pre-IAP-initialization content. Update later in OnInitialized.
            var description = string.Format("{0} - {1}", item.id, item.type);
            Debug.LogError(description);
            GetDropdown().options.Add(new Dropdown.OptionData(description));
        }

        // Ensure I render the selected list element
        GetDropdown().RefreshShownValue();

        GetDropdown().onValueChanged.AddListener((int selectedItem) => {
            Debug.Log("OnClickDropdown item " + selectedItem);
            m_SelectedItemIndex = selectedItem;
        });

        // Initialize my button event handling
        GetBuyButton().onClick.AddListener(() => {
            m_Controller.InitiatePurchase(m_Controller.products.all[m_SelectedItemIndex]); 
        });

        GetRestoreButton().onClick.AddListener(() => { 
            m_AppleExtensions.RestoreTransactions(OnTransactionsRestored);
        });
    }

    public void UpdateHistoryUI()
    {
        if (m_Controller == null)
        {
            return;
        }

        var itemText = "Item\n\n";
        var countText = "Count\n\n";

        if (m_Controller != null)
        {
            for (int t = 0; t < m_Controller.products.all.Length; t++)
            {
                var item = m_Controller.products.all [t];

                // Collect history status report

                itemText += "\n\n" + item.definition.id;
                countText += "\n\n" + item.hasReceipt.ToString();
            }
        }

        // Show history
        GetText(false).text = itemText;
        GetText(true).text = countText;
    }

    public void Update()
    {
    }

    private Dropdown GetDropdown()
    {
        return GameObject.Find("Dropdown").GetComponent<Dropdown>();
    }

    private Button GetBuyButton()
    {
        return GameObject.Find("Buy").GetComponent<Button>();
    }

    private Button GetRestoreButton()
    {
        return GameObject.Find("Restore").GetComponent<Button>();
    }

    private Text GetText(bool right)
    {
        var which = right ? "TextR" : "TextL";
        return GameObject.Find(which).GetComponent<Text>();
    }
}
