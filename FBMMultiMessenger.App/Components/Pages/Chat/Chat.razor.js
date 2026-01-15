
(function () {
    let messageContainer = document.querySelector(".messages-container"); // Actual chat messages.
    let messageList = messageContainer.querySelector("#messagesList");
    let inputWrapper = document.querySelector(".input-wrapper");
    let arrowDownButton = document.querySelector(".arrow-down");

    window.registerEnterHandler = (ref) => {
        window.dotnetHelper = ref;
    }

    window.handleNewMessage = () => {
        var isHalfWay = isHalfWayScrolled();
        if (isHalfWay) {
            arrowDownButton?.classList.add("active");
        } else {
            messageContainer.scrollTo({
                top: messageContainer.scrollHeight,
                behavior: "smooth"
            });
        }
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



    messageContainer.addEventListener('scroll', () => {

        var data = isHalfWayScrolled();

        if (data) {
            arrowDownButton.style.display = "block";
        }
        else {
            arrowDownButton.style.display = "none";
        }
    });

    function isHalfWayScrolled() {
        const viewportHeight = messageContainer.clientHeight;   // visible height
        const totalHeight = messageContainer.scrollHeight;      // total content height
        const scrolled = messageContainer.scrollTop;            // how far user scrolled from top

        const scrollableDistance = totalHeight - viewportHeight;

        // Show arrow if user is more than 15% away from bottom
        if (scrolled < scrollableDistance * 0.85) {
            return true;
        } else {
            return false;
        }
    }

    arrowDownButton.addEventListener('click', () => {

        arrowDownButton.classList.remove("active");
        messageContainer.scrollTo({
            top: messageContainer.scrollHeight,
            behavior: "smooth"
        });
    });


})();
