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
    stopAllMedia: function () {
        try {
            document.querySelectorAll("video, audio").forEach(m => {
                m.pause();
                m.currentTime = 0;
            });
        } catch (e) {
            console.error("Something went wrong while stopping all media.");
            return null;
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
            footer = null,
            importData = null // New parameter for import results
        } = options;

        let htmlContent = message;

        // If importData is provided, build enhanced HTML
        if (importData && importData.totalProcessed > 0) {
            const { totalProcessed, successfullyValidated, totalSkipped, skippedAccounts } = importData;

            // Build summary section
            let summaryHtml = '<div style="margin-bottom: 20px;">';
            if (successfullyValidated > 0) {
                summaryHtml += `<p><strong style="color: #27ae60;">✓ ${successfullyValidated} accounts imported successfully</strong></p>`;
            }
            if (totalSkipped > 0) {
                summaryHtml += `<p><strong style="color: #e74c3c;">✗ ${totalSkipped} accounts skipped</strong></p>`;
            }
            summaryHtml += '</div>';

            // If there are skipped accounts, show details
            if (skippedAccounts && skippedAccounts.length > 0) {
                // Group by reason
                const reasonGroups = skippedAccounts.reduce((acc, account) => {
                    if (!acc[account.reason]) {
                        acc[account.reason] = [];
                    }
                    acc[account.reason].push(account);
                    return acc;
                }, {});

                let detailsHtml = '<div style="text-align: left; max-height: 300px; overflow-y: auto; padding: 10px; background-color: #f8f9fa; border-radius: 5px;">';

                Object.entries(reasonGroups).forEach(([reason, accounts]) => {
                    const reasonText = formatReason(reason);
                    detailsHtml += `
                    <div style="margin-bottom: 15px;">
                        <strong style="color: #e74c3c;">${reasonText} (${accounts.length})</strong>
                        <ul style="margin: 5px 0; padding-left: 20px; font-size: 0.9em; color: #495057;">
                `;

                    accounts.forEach(account => {
                        const nameText = account.name || 'Unknown';
                        const proxyInfo = account.proxyId ? ` | Proxy: ${account.proxyId}` : '';
                        detailsHtml += `<li>${nameText}${proxyInfo}</li>`;
                    });

                    detailsHtml += '</ul></div>';
                });

                detailsHtml += '</div>';

                htmlContent = summaryHtml + detailsHtml;
            } else {
                // Only success message
                htmlContent = summaryHtml;
            }
        }

        const config = {
            icon,
            title,
            showCancelButton: showCancelButton,
            confirmButtonText: confirmButtonText,
            cancelButtonText: cancelButtonText,
            width: importData && importData.totalSkipped > 0 ? '650px' : '400px'
        };

        // Use html if we have importData, otherwise use text
        if (importData) {
            config.html = htmlContent;
        } else {
            config.text = htmlContent;
        }

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
            previewUrl: URL.createObjectURL(file),
        }));

    },

    revokePreviewUrl: (url) => {
        URL.revokeObjectURL(url);
    },

    previewAndCompressImages: async () => {
        const input = document.getElementById("fileInput");
        if (!input || !input.files) return [];

        const results = [];

        for (let i = 0; i < input.files.length; i++) {
            const file = input.files[i];
            const isVideo = file.type.startsWith("video/");

            const previewUrl = URL.createObjectURL(file);

            // Compress image if not video
            let compressedBytes = null;
            if (!isVideo && file.type.startsWith("image/")) {
                compressedBytes = await compressImage(file);
            }

            results.push({
                index: i,
                name: file.name,
                isVideo,
                previewUrl,
                compressedBytes
            });
        }

        return results;
    }
};
async function compressImage(file) {
    //const start = performance.now();
    const MAX_WIDTH = 1920;
    const QUALITY = 0.7;
    const MIN_SIZE_TO_COMPRESS = 50 * 1024; // 50KB threshold
    const originalSize = file.size;

    // Skip compression for small files - they're likely already optimized
    if (originalSize < MIN_SIZE_TO_COMPRESS) {
        console.log(
            `[Skipped Compression] ${file.name} (${Math.round(originalSize / 1024)}KB) ` +
            `- Too small to benefit from compression`
        );

        // Return original file as bytes
        const arrayBuffer = await file.arrayBuffer();
        return new Uint8Array(arrayBuffer);
    }

    const bitmap = await createImageBitmap(file);
    let { width, height } = bitmap;

    // Skip compression if image is already small enough
    if (width <= MAX_WIDTH) {
        console.log(
            `[Skipped Compression] ${file.name} (${width}x${height}) ` +
            `- Already within target dimensions`
        );

        const arrayBuffer = await file.arrayBuffer();
        return new Uint8Array(arrayBuffer);
    }

    // Calculate new dimensions
    height = Math.round(height * (MAX_WIDTH / width));
    width = MAX_WIDTH;

    const canvas = document.createElement("canvas");
    canvas.width = width;
    canvas.height = height;
    const ctx = canvas.getContext("2d");
    ctx.drawImage(bitmap, 0, 0, width, height);

    const blob = await new Promise(resolve =>
        canvas.toBlob(resolve, "image/jpeg", QUALITY)
    );

    const arrayBuffer = await blob.arrayBuffer();
    const byteArray = new Uint8Array(arrayBuffer);
    const compressedSize = byteArray.length;

    // If compression made it bigger, use original
    if (compressedSize >= originalSize) {
        console.log(
            `[Compression Rejected] ${file.name} | ` +
            `${Math.round(originalSize / 1024)}KB → ${Math.round(compressedSize / 1024)}KB | ` +
            `Kept original (compression increased size)`
        );

        const originalBuffer = await file.arrayBuffer();
        return new Uint8Array(originalBuffer);
    }

    // Calculate compression percentage
    //const reduction = ((originalSize - compressedSize) / originalSize * 100).toFixed(1);
    // const compressionRatio = (compressedSize / originalSize * 100).toFixed(1);

    //const end = performance.now();

    //console.log(
    //    `[Canvas Compression] ${Math.round(originalSize / 1024)}KB → ` +
    //    `${Math.round(compressedSize / 1024)}KB | ` +
    //    `Reduced by ${reduction}% (${compressionRatio}% of original) | ` +
    //    `Time: ${(end - start).toFixed(1)}ms | ` +
    //    `${bitmap.width}x${bitmap.height} → ${width}x${height}`
    //);

    return byteArray;
}

function formatReason(reason) {
    const reasonMap = {
        'DuplicateCookie': '🔄 Duplicate Cookies',
        'InvalidProxyId': '❌ Invalid Proxy ID',
        'UnauthorizedProxy': '🚫 Unauthorized Proxy',
        'MissingRequiredProxy': '⚠️ Missing Required Proxy',
        'InvalidCookie': '🍪 Invalid Cookie',
        'AccountAlreadyExists': '📋 Account Already Exists'
    };
    return reasonMap[reason] || reason;
}




