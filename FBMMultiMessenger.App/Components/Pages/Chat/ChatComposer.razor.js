(function () {
    let inputWrapper = document.querySelector(".input-wrapper");
    window.registerEnterHandler = (ref) => {
        window.dotnetHelper = ref;
    };

    const observer = new MutationObserver((mutations) => {
        for (const mutation of mutations) {
            if (mutation.type === "childList" && mutation.target === inputWrapper) {
                const messageInput = document.querySelector(".message-input");
                if (!messageInput) {
                    console.log("Cannot find message input");
                    return;
                }
                messageInput.onkeydown = async e => {
                    if (e.key === "Enter" && !e.shiftKey) {
                        console.log("Enter pressed")
                        let message = messageInput.value;
                        messageInput.value = "";
                        e.preventDefault();
                        await window.dotnetHelper?.invokeMethodAsync('HandleEnterKey', message);
                    }
                };

            }
        }
    });

    observer.observe(inputWrapper, { childList: true, subtree: true });

})();
