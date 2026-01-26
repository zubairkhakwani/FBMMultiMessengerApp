(function () {
    let pressTimer;
    let currentMenu = null;

    // get fresh DOM references
    function getElements() {
        return {
            messageContainer: document.querySelector(".messages-container"),
            messageList: document.querySelector("#messagesList"),
            arrowDownBtn: document.querySelector(".arrow-down"),
            messages: document.querySelectorAll('.message'),
            copyBtns: document.querySelectorAll('.copy-btn')
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
        const { messageContainer, messageList } = getElements();
        if (!messageContainer || !messageList) {
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

                }
            }
        });

        observer.observe(messageContainer, { childList: true, subtree: true });

    }

    function initializeEventListeners() {
        const { messageContainer, arrowDownBtn, messages, copyBtns } = getElements();


        // Click handler for focusing message input
        document.addEventListener('click', function (e) {
            const messageInput = document.querySelector('.message-input');
            const searchInput = document.querySelector('.search-input');
            const { arrowDownBtn } = getElements();


            // Close menu when clicking outside
            if (!e.target.closest('.message') && !e.target.closest('.copy-menu')) {
                if (currentMenu) {
                    currentMenu.classList.remove('show');
                    currentMenu = null;
                }
            }

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

        messages.forEach(message => {
            const menu = message.nextElementSibling;
            console.log(messages);
            // Mouse events (desktop)
            message.addEventListener('mousedown', (e) => {
                e.preventDefault();
                pressTimer = setTimeout(() => {
                    showMenu(menu);
                }, 500);
            });

            message.addEventListener('mouseup', () => {
                clearTimeout(pressTimer);
            });

            message.addEventListener('mouseleave', () => {
                clearTimeout(pressTimer);
            });

            // Touch events (mobile)
            message.addEventListener('touchstart', (e) => {
                pressTimer = setTimeout(() => {
                    showMenu(menu);
                }, 500);
            });

            message.addEventListener('touchend', () => {
                clearTimeout(pressTimer);
            });

            message.addEventListener('touchcancel', () => {
                clearTimeout(pressTimer);
            });
        });

        // Handle copy button
        copyBtns.forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                const messageText = btn.closest('.message-wrapper').querySelector('.message-text').textContent;
                navigator.clipboard.writeText(messageText).then(() => {
                    showToast('Message copied! ✓');
                });
                if (currentMenu) {
                    currentMenu.classList.remove('show');
                    currentMenu = null;
                }
            });
        });

    }

    // Initialize on load
    initializeObservers();
    initializeEventListeners();

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

    function showMenu(menu) {
        if (currentMenu && currentMenu !== menu) {
            currentMenu.classList.remove('show');
        }
        menu.classList.add('show');
        currentMenu = menu;
    }
})();