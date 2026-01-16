(function () {
    let messageContainer = document.querySelector(".messages-container"); // Actual chat messages.
    let messageList = messageContainer.querySelector("#messagesList");
    let inputWrapper = document.querySelector(".input-wrapper");
    var arrowDownBtn = document.querySelector(".arrow-down");



    const observer = new MutationObserver((mutations) => {
        for (const mutation of mutations) {
            if (mutation.type === "childList") {
                if (mutation.target === messageList) {

                    const messages = messageList.querySelectorAll('.message');
                    const lastMessage = messages[messages.length - 1];

                    const scrollToBottom = lastMessage?.getAttribute('data-scroll-to-bottom');

                    if (scrollToBottom === "True") {
                        messageContainer.scrollTop = messageContainer.scrollHeight;
                    }
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

        // If clicked on search or arrow down button, let it focus naturally
        if (e.target === searchInput || searchInput?.contains(e.target) || e.target === arrowDownBtn || arrowDownBtn?.contains(e.target)) {
            return;
        }
        // Otherwise, focus message input
        if (messageInput && document.activeElement !== messageInput) {
            messageInput.focus();
        }
    });

    messageContainer.addEventListener('scroll', () => {
        var halfWaf = isHalfWayScrolled();

        if (halfWaf) {
            arrowDownBtn.style.display = "block";
        }
        else {
            arrowDownBtn.style.display = "none";
            arrowDownBtn?.classList.remove("active");
        }
    });


    arrowDownBtn.addEventListener('click', () => {
        arrowDownBtn.classList.remove("active");
        messageContainer.scrollTo({
            top: messageContainer.scrollHeight,
            behavior: "smooth"
        });
    });

    function isHalfWayScrolled() {
        const viewportHeight = messageContainer.clientHeight;   // visible height
        const totalHeight = messageContainer.scrollHeight;      // total content height
        const scrolled = messageContainer.scrollTop;            // how far user scrolled from top

        const scrollableDistance = totalHeight - viewportHeight;

        //user is more than 15% away from bottom
        return scrolled < scrollableDistance * 0.85;
    }

    //Called via Razor component code
    window.registerEnterHandler = (ref) => {
        window.dotnetHelper = ref;
    }

    window.handleNewMessage = () => {
        var isHalfWay = isHalfWayScrolled();
        if (isHalfWay) {
            arrowDownBtn?.classList.add("active");
        } else {
            messageContainer.scrollTo({
                top: messageContainer.scrollHeight,
                behavior: "smooth"
            });
        }
    }

    window.hideArrowDownBtn = () => {
        arrowDownBtn.style.display = "none";
        arrowDownBtn?.classList.remove("active");
    }
})();
