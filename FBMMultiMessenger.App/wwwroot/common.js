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

    downloadAccountsFormat: async function (text) {
        try {
            let url = '/templates/accounts-template.csv';
            let fileName = 'accounts-template.csv';
            const link = document.createElement('a');
            link.href = url;
            link.download = fileName;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);

        } catch (err) {
            return false;
        }
    },

    showSweetAlert: function (options = {}) {
        const {
            title = "Error",
            message = "Something went wrong.",
            icon = "error",
            confirmButtonText = "Yes",
            showCancelButton = false,
            cancelButtonText = "No",
            footer = null
        } = options;

        const config = {
            icon,
            title,
            text: message,
            showCancelButton: showCancelButton,
            confirmButtonText: confirmButtonText,
            cancelButtonText: cancelButtonText,
        };

        if (footer) {
            if (footer.link) {
                config.footer = `<a href="${footer.link}">${footer.text || 'Help'}</a>`;
            } else {
                config.footer = footer.text;
            }
        }

        return Swal.fire(config).then((result) => result.isConfirmed);
    },

    createPreviewUrlsFromInput: () => {
        let mediaInput = document.getElementById('fileInput');
        return Array.from(mediaInput?.files).map((file, index) => ({
            index,
            name: file.name,
            isVideo: file.type.startsWith("video/"),
            previewUrl: URL.createObjectURL(file)
        }));
       
    },

    revokePreviewUrl: (url) => {
        URL.revokeObjectURL(url);
    },

};



