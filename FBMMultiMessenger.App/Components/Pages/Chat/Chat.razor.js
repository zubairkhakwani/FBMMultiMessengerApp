(function () {
    let messageContainer = document.querySelector(".messages-container"); // Actual chat messages.
    let messageList = messageContainer.querySelector("#messagesList");
    let inputWrapper = document.querySelector(".input-wrapper");

    window.registerEnterHandler = (ref) => {
        window.dotnetHelper = ref;
    };

    const observer = new MutationObserver((mutations) => {
        for (const mutation of mutations) {
            if (mutation.type === "childList") {
                if (mutation.target === messageList) {

                    messageContainer.scrollTo({
                        top: messageContainer.scrollHeight,
                        behavior: "smooth"
                    });

                    let lastMessage = messageList.lastElementChild;
                    lastMessage?.classList?.add("new-message");
                    setTimeout(() => {
                        lastMessage?.classList?.remove('new-message');
                    }, 600)
                }
                else if (mutation.target === inputWrapper) {
                    const messageInput = document.querySelector(".message-input");
                    if (!messageInput) { console.log("Cannot find message input"); return; }
                    messageInput.onkeydown = async e => {
                        if (e.key === "Enter" && !e.shiftKey) {

                            let message = messageInput.value;
                            messageInput.value = "";
                            e.preventDefault();
                            await window.dotnetHelper?.invokeMethodAsync('HandleEnterKey', message);
                        }
                    };
                }
            }
        }
    });
    observer.observe(messageContainer, { childList: true, subtree: true, });
    observer.observe(inputWrapper, { childList: true, subtree: true, })


    document.addEventListener('click', function (e) {
        const messageInput = document.querySelector('.message-input');
        const searchInput = document.querySelector('.search-input');

        // If clicked on search, let it focus naturally
        if (e.target === searchInput || searchInput?.contains(e.target)) {
            return;
        }

        // Otherwise, focus message input
        if (messageInput && document.activeElement !== messageInput) {
            messageInput.focus();
        }
    });
})();
