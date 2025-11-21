window.myInterop = {
    setItem: function (key, value) {
        localStorage.setItem(key, value);
    },
    getItem: function (key) {
        return localStorage.getItem(key);
    },
    removeItem: function (key) {
        localStorage.removeItem(key);
    },
    playNotificationSound: function (volume) {
        const audio = new Audio('/sounds/threads.mp3');
        audio.volume = volume;
        audio.play();
    },
    handleMessageFailed: function () {
        console.log("Message failed to send");
    },
    createObjectURL: function (file) {
        try {
            console.log(file.object);
            if (file?.object) {
                return URL.createObjectURL(file.object);
            } else if (file instanceof Blob) {
                return URL.createObjectURL(file);
            } else {
                console.warn("Invalid file type passed to createObjectURL:", file);
                return null;
            }
        } catch (e) {
            console.error("Error creating object URL:", e);
            return null;
        }
    },
    copyToClipboard: async function (text) {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch (err) {
            return false;
        }
    },
    showSweetAlert: function (title, message, showFooter = false, footerText = "Help", footerLink = "#", icon = "error", confirmBtnText = "Yes", showCancelBtn = false, cancelBtnText = "No") {
        const config = {
            icon: icon,
            title: title || "Error",
            text: message || "Something went wrong.",
            showCancelButton: showCancelBtn,
            confirmButtonText: confirmBtnText,
            cancelButtonText: cancelBtnText,
        };

        if (showFooter) {
            config.footer = `<a >${footerText}</a>`;
        }

        return Swal.fire(config).then((result) => {
            return result.isConfirmed;
        });
    }
};



