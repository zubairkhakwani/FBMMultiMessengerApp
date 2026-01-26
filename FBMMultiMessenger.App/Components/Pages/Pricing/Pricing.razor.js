//(function () {
//    const purchaseBtn = document.getElementById('purchaseBtn');

//    purchaseBtn.addEventListener('click', function () {
//        bankingSection.classList.remove('hidden');
//        uploadSection.classList.remove('hidden');

//        bankingSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
//    });

//    let currentBilling = 'monthly';

//    function getCurrentVolumeTier(accounts) {
//        return volumePricing.find(tier => accounts >= tier.min && accounts <= tier.max);
//    }

//    function updatePricing(option) {
//        const accounts = parseInt(document.getElementById(`accounts${option}`).value) || 1;
//        const tier = getCurrentVolumeTier(accounts);
//        const currentBilling = option === 1 ? currentBilling1 : currentBilling2;
//        const billing = billingMultipliers[currentBilling];

//        // Update active tier highlight
//        const tierItems = document.querySelectorAll(`#tierList${option} > div`);
//        tierItems.forEach((item, index) => {
//            if (volumePricing[index] === tier) {
//                item.classList.add('active');
//            } else {
//                item.classList.remove('active');
//            }
//        });

//        // Calculate final price
//        const basePrice = tier.price;
//        const finalPricePerAccount = Math.round(basePrice * billing.multiplier);
//        const totalCost = finalPricePerAccount * accounts;

//        // Update display
//        document.getElementById(`pricePerAccount${option}`).textContent = `PKR ${finalPricePerAccount}`;
//        document.getElementById(`totalAccounts${option}`).textContent = accounts;
//        document.getElementById(`totalCost${option}`).textContent = `PKR ${totalCost.toLocaleString()}`;
//        document.getElementById(`costLabel${option}`).textContent = `${billing.label} Cost`;

//        // Update banner
//        let bannerText = '';
//        if (tier.discount > 0 && billing.discount > 0) {
//            const totalDiscount = tier.discount + billing.discount;
//            bannerText = `🎉 Double Savings! ${tier.discount}% Volume + ${billing.discount}% ${billing.label} = ${totalDiscount}% Total!`;
//        } else if (tier.discount > 0) {
//            bannerText = `🎉 You're getting ${tier.discount}% Volume Discount!`;
//        } else if (billing.discount > 0) {
//            bannerText = `⚡ You're saving ${billing.discount}% with ${billing.label} billing!`;
//        } else {
//            bannerText = `💰 Current ${billing.label} Price`;
//        }

//        document.getElementById(`banner${option}`).innerHTML = `${bannerText}<div class="big-savings">Total: PKR ${totalCost.toLocaleString()}</div>`;

//        // OPTION 1: Update tier prices dynamically
//        if (option === 1) {
//            const tierPrices = document.querySelectorAll('#tierList1 .tier-price');
//            volumePricing.forEach((tier, index) => {
//                const adjustedPrice = Math.round(tier.price * billing.multiplier);
//                tierPrices[index].textContent = `PKR ${adjustedPrice} / account`;
//            });
//        }
//    }

//    // Tab switching
//    document.querySelectorAll('.billing-tab').forEach(tab => {
//        tab.addEventListener('click', function (e) {

//            console.log(e);
//            const period = this.getAttribute('data-period');

//            // Remove active from siblings
//            this.parentElement.querySelectorAll('.billing-tab').forEach(t => t.classList.remove('active'));
//            this.classList.add('active');

//            if (option === 1) {
//                currentBilling = period;
//            }

//            updatePricing(option);
//        });
//    });

//    document.getElementById('accounts').addEventListener('input', () => updatePricing());

//    // Initial update
//    updatePricing(1);
//    updatePricing(2);
//}());

