const purchaseBtn = document.getElementById('purchaseBtn');

purchaseBtn.addEventListener('click', function () {
    bankingSection.classList.remove('hidden');
    uploadSection.classList.remove('hidden');

    bankingSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
});