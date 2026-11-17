(() => {
    const launcher = document.getElementById("gmachGuideLauncher");
    const dialog = document.getElementById("gmachGuide");
    const log = document.getElementById("gmachGuideLog");
    const status = document.getElementById("gmachGuideStatus");
    const form = document.getElementById("gmachGuideForm");
    const input = document.getElementById("gmachGuideInput");
    const send = document.getElementById("gmachGuideSend");
    const filler = new Set("שלום היי אני אנחנו מחפש מחפשת מחפשים צריך צריכה צריכים רוצה רוצים אפשר האם יש לכם גמח איפה ניתן להשאיל השאלה לשאול למצוא פריט באזור בעיר של את לי תודה בבקשה".split(" "));
    let product = "";
    let city = "";
    let requestNumber = 0;

    function clean(value) {
        return normalizeCity(value).toLowerCase().replace(/ג["״׳']מ["״׳']?ח|גמ["״׳']ח/g, "גמח")
            .replace(/[^\p{L}\p{N}]+/gu, " ").trim();
    }

    function searchable(value) {
        return clean(value).replaceAll("כיסא", "כסא").replaceAll("גלגליים", "גלגלים");
    }

    function message(text, isUser = false) {
        const bubble = document.createElement("p");
        bubble.className = "gmach-guide-message" + (isUser ? " gmach-guide-message-user" : "");
        bubble.textContent = text;
        log.append(bubble);
        log.scrollTop = log.scrollHeight;
    }

    function setBusy(busy) {
        status.textContent = busy ? "מחפש במאגר הגמ״חים…" : "";
        send.disabled = busy;
    }

    function reset() {
        requestNumber++;
        product = "";
        city = "";
        input.value = "";
        log.replaceChildren();
        setBusy(false);
        message("היי! מה תרצו לשאול ובאיזו עיר? אחפש בשבילכם במאגר של האתר.");
    }

    function showResults(results) {
        const list = document.createElement("div");
        list.className = "gmach-guide-results";
        for (const { group, item } of results) {
            const button = document.createElement("button");
            button.type = "button";
            button.className = "gmach-guide-result";
            const title = document.createElement("strong");
            title.textContent = [group.name, normalizeCity(group.city)].filter(Boolean).join(" · ");
            const detail = document.createElement("small");
            detail.textContent = item ? item.name + " · עותקים זמינים: " + item.availableCopies : "פתיחת דף הגמ״ח";
            button.append(title, detail);
            button.addEventListener("click", () => {
                dialog.close();
                showGmach(group.id);
            });
            list.append(button);
        }
        log.append(list);
        log.scrollTop = log.scrollHeight;
    }

    launcher.addEventListener("click", () => {
        dialog.showModal();
        input.focus();
    });
    document.getElementById("gmachGuideClose").addEventListener("click", () => dialog.close());
    document.getElementById("gmachGuideReset").addEventListener("click", () => {
        reset();
        input.focus();
    });

    form.addEventListener("submit", async event => {
        event.preventDefault();
        const question = input.value.trim();
        if (!question || send.disabled) return;
        const currentRequest = ++requestNumber;
        message(question, true);
        input.value = "";
        setBusy(true);

        try {
            const directory = await apiRequest("/gmachs");
            if (currentRequest !== requestNumber) return;
            let terms = clean(question);
            const cities = [...new Set(directory.map(group => clean(group.city)).filter(Boolean))]
                .sort((a, b) => b.length - a.length);
            const cityName = cities.find(name => {
                const words = " " + terms + " ";
                return words.includes(" " + name + " ") || words.includes(" ב" + name + " ");
            });
            if (terms === "בכל הארץ" || terms === "כל הארץ" || terms === "כל עיר") {
                city = "";
                terms = "";
            } else if (cityName) {
                city = cityName;
                terms = (" " + terms + " ").replace(" ב" + cityName + " ", " ").replace(" " + cityName + " ", " ").trim();
            }

            const nextProduct = terms.split(" ").filter(word => !filler.has(word)).join(" ");
            if (nextProduct) product = nextProduct;
            if (!product) {
                message(city ? "מה תרצו לשאול בעיר הזאת? כתבו את שם הפריט." : "כתבו שם של פריט או גמ״ח. אפשר להוסיף עיר.");
                return;
            }

            const matchingCity = group => !city || clean(group.city) === city;
            const term = searchable(product);
            const namedGroups = directory.filter(group => matchingCity(group) && searchable(group.name).includes(term));
            const variants = [...new Set([product, product.replaceAll("כיסא", "כסא"), product.replaceAll("כסא", "כיסא")])];
            const lists = await Promise.all(variants.map(query => apiRequest("/gmachs?" + new URLSearchParams({ query }))));
            const groups = new Map(lists.flat().filter(matchingCity).map(group => [group.id, group]));
            if (currentRequest !== requestNumber) return;

            const checks = await Promise.allSettled([...groups.values()].slice(0, 8).map(async group => {
                const details = await apiRequest("/gmachs/" + encodeURIComponent(group.id));
                const item = details.items.find(item => item.isActive && item.availableCopies > 0 &&
                    (searchable(item.name).includes(term) || searchable(item.description).includes(term)));
                return { group, item };
            }));
            if (currentRequest !== requestNumber) return;
            const matches = checks.filter(check => check.status === "fulfilled" && check.value.item)
                .map(check => check.value).slice(0, 5);
            if (matches.length) {
                message("מצאתי פריטים זמינים" + (city ? " ב" + city : "") + ". אפשר לפתוח את דף הגמ״ח בלחיצה.");
                showResults(matches);
            } else if (namedGroups.length) {
                message("מצאתי גמ״חים שהשם שלהם מתאים לחיפוש.");
                showResults(namedGroups.slice(0, 5).map(group => ({ group })));
            } else if (checks.some(check => check.status === "rejected")) {
                message("לא הצלחתי לבדוק את כל התוצאות כרגע. אפשר לנסות שוב.");
            } else {
                message(city
                    ? "לא מצאתי פריט זמין שתואם לחיפוש בעיר הזאת. אפשר לכתוב שם אחר של הפריט, עיר אחרת או ״כל הארץ״."
                    : "לא מצאתי פריט זמין שתואם לחיפוש. נסו לכתוב את שם הפריט בקצרה, למשל ״הליכון״.");
            }
        } catch {
            if (currentRequest === requestNumber) message("לא הצלחתי לטעון את נתוני האתר כרגע. אפשר לנסות שוב.");
        } finally {
            if (currentRequest === requestNumber) setBusy(false);
        }
    });

    reset();
})();
