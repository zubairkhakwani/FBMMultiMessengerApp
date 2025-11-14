var digits = document.querySelectorAll(".code-digit");

digits.forEach((digit, index) => {
    digit.addEventListener("keydown", (e) => {
        console.log(e);
        if (e.key === "Backspace") {
            if (digit.value === "" && index > 0) {
                digits[index - 1].focus();
            }
        }
    });
});

digits.forEach((digit, index) => {
    digit.addEventListener("input", () => {
        if (digit.value && index < digits.length - 1) {
            digits[index + 1].focus();
        }
    });
});
