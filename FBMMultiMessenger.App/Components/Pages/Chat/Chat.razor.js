(function () {
    // get fresh DOM references
    function getElements() {
        return {
            messageContainer: document.querySelector(".messages-container"),
            messageList: document.querySelector("#messagesList"),
            inputWrapper: document.querySelector(".input-wrapper"),
            arrowDownBtn: document.querySelector(".arrow-down")
        };
    }

    function isHalfWayScrolled() {
        const { messageContainer } = getElements();
        if (!messageContainer) return false;

        const viewportHeight = messageContainer.clientHeight;
        const totalHeight = messageContainer.scrollHeight;
        const scrolled = messageContainer.scrollTop;
        const scrollableDistance = totalHeight - viewportHeight;

        return scrolled < scrollableDistance * 0.85;
    }

    function initializeObservers() {
        const { messageContainer, messageList, inputWrapper } = getElements();
        if (!messageContainer || !messageList || !inputWrapper) {
            console.warn("Required elements not found for observers");
            return;
        }

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
            }
        });

        observer.observe(messageContainer, { childList: true, subtree: true });
        observer.observe(inputWrapper, { childList: true, subtree: true });
    }

    function initializeEventListeners() {
        const { messageContainer, arrowDownBtn } = getElements();

        // Click handler for focusing message input
        document.addEventListener('click', function (e) {
            const messageInput = document.querySelector('.message-input');
            const searchInput = document.querySelector('.search-input');
            const { arrowDownBtn } = getElements();

            if (e.target === searchInput || searchInput?.contains(e.target) ||
                e.target === arrowDownBtn || arrowDownBtn?.contains(e.target)) {
                return;
            }

            if (messageInput && document.activeElement !== messageInput) {
                messageInput.focus();
            }
        });

        // Scroll handler
        if (messageContainer) {
            messageContainer.addEventListener('scroll', () => {
                const { arrowDownBtn } = getElements();
                if (!arrowDownBtn) return;

                const halfWay = isHalfWayScrolled();

                if (halfWay) {
                    arrowDownBtn.classList.add("visible");
                } else {
                    arrowDownBtn.classList.remove("visible");
                    arrowDownBtn.classList.remove("active");
                }
            });
        }
                                                                                     
        // Arrow down button click handler
        if (arrowDownBtn) {
            arrowDownBtn.addEventListener('click', () => {
                const { messageContainer, arrowDownBtn } = getElements();
                if (!messageContainer || !arrowDownBtn) return;

                arrowDownBtn.classList.remove("active");
                messageContainer.scrollTo({
                    top: messageContainer.scrollHeight,
                    behavior: "smooth"
                });
            });
        }
    }

    // Initialize on load
    initializeObservers();
    initializeEventListeners();

    // Public API
    window.registerEnterHandler = (ref) => {

        window.dotnetHelper = ref;
    };

    window.handleNewMessage = () => {
        const { messageContainer, arrowDownBtn } = getElements();

        if (!messageContainer || !arrowDownBtn) {
            return;
        }

        const isHalfWay = isHalfWayScrolled();


        if (isHalfWay) {
            arrowDownBtn.classList.add("active");
        } else {
            messageContainer.scrollTo({
                top: messageContainer.scrollHeight,
                behavior: "smooth"
            });
        }
    };

    window.hideArrowDownBtn = () => {
        const { arrowDownBtn } = getElements();
        if (!arrowDownBtn) return;

        arrowDownBtn.classList.remove("visible");
        arrowDownBtn.classList.remove("active");
    };
})();