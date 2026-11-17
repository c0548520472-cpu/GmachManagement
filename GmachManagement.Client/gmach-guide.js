(() => {
    const launcher = document.getElementById("gmachGuideLauncher");
    const dialog = document.getElementById("gmachGuide");
    const log = document.getElementById("gmachGuideLog");
    const status = document.getElementById("gmachGuideStatus");
    const form = document.getElementById("gmachGuideForm");
    const input = document.getElementById("gmachGuideInput");
    const send = document.getElementById("gmachGuideSend");
    const filler = new Set("שלום היי אני אנחנו מחפש מחפשת מחפשים צריך צריכה צריכים רוצה רוצים אפשר האם יש לכם גמח איפה ניתן להשאיל השאלה לשאול למצוא פריט באזור בעיר של את לי תודה בבקשה דחוף היום מחר עכשיו מחפשת היכן בשביל עבור לקבל לקחת תמצאו תמצא מעוניין מעוניינת אולי במקרה ומה לגבי במקום עדיף דווקא רק משהו כזה אותו באזורכם קרוב ליד סביבות נמצא נמצאת באזורי".split(" "));
    let product = "";
    let city = "";
    let requestNumber = 0;
    let awaitingCity = false;
    let lastMatches = [];
    let shownMatches = 0;

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
        status.textContent = busy ? "רגע, אני בודק מה זמין…" : "";
        send.disabled = busy;
    }

    function reset() {
        requestNumber++;
        product = "";
        city = "";
        awaitingCity = false;
        lastMatches = [];
        shownMatches = 0;
        input.value = "";
        log.replaceChildren();
        setBusy(false);
        message("היי! מה מחפשים היום? אפשר לכתוב לי כמו שמדברים, למשל ״אני צריכה הליכון באזור ירושלים״.");
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

    function showNextMatches() {
        const next = lastMatches.slice(shownMatches, shownMatches + 5);
        shownMatches += next.length;
        showResults(next);
    }

    async function reply(question) {
        if (!question || send.disabled) return;
        const currentRequest = ++requestNumber;
        message(question, true);
        input.value = "";
        let terms = clean(question);
        if (/^(שלום|היי|הי|אהלן|בוקר טוב|ערב טוב|מה נשמע)$/.test(terms)) {
            message(product ? "היי! אפשר להמשיך עם החיפוש, או לכתוב שם של פריט אחר." : "היי! בשמחה אעזור למצוא גמ״ח. מה מחפשים?");
            return;
        }
        if (/^(תודה( רבה| לך)?|מעולה|מצוין|סבבה תודה|אחלה תודה)$/.test(terms)) {
            message("בשמחה! אני כאן אם תרצו לחפש עוד משהו.");
            return;
        }
        if (/^(איך (אפשר )?(משאילים|להשאיל|לשאול|מבקשים|לבקש)( פריט)?|איך (זה עובד|יוצרים קשר|אפשר ליצור קשר)|מה עושים עכשיו)$/.test(terms)) {
            message("פתחו את דף הגמ״ח ובחרו ״השאלה״ ליד הפריט. אפשר גם לפתוח את הצ׳אט של הגמ״ח כדי לשאול את הבעלים.");
            return;
        }
        if (/^(כמה( עותקים)?( יש| זמינים)?|יש במלאי|זה זמין|יש זמין)$/.test(terms) && lastMatches.length) {
            message("מספר העותקים הזמינים מופיע ליד כל פריט בתוצאות.");
            showResults(lastMatches.slice(0, 5));
            return;
        }
        const wantMore = /^(יש עוד( אפשרויות)?|עוד( אפשרויות| תוצאות)?|תראה לי עוד)$/.test(terms);
        if (wantMore && shownMatches < lastMatches.length) {
            message("כן, הנה עוד אפשרויות שמצאתי.");
            showNextMatches();
            return;
        }
        if (wantMore && !city) {
            message(product ? "אלה האפשרויות שמצאתי בחיפוש הזה. אפשר לנסות שם אחר של הפריט." : "מה תרצו לחפש? כתבו שם של פריט או גמ״ח.");
            return;
        }
        setBusy(true);

        try {
            const directory = await apiRequest("/gmachs");
            if (currentRequest !== requestNumber) return;
            const cities = [...new Set(directory.map(group => clean(group.city)).filter(Boolean))]
                .sort((a, b) => b.length - a.length);
            const cityName = cities.find(name => {
                const words = " " + terms + " ";
                return words.includes(" " + name + " ") || words.includes(" ב" + name + " ");
            });
            const everywhere = wantMore || /^(ב?כל הארץ|ב?כל עיר|לא משנה( לי)?( העיר)?|אין העדפה|איפה שיש|תחפש בכל הארץ)$/.test(terms);
            const locationOnly = /^(ומה עם|מה לגבי|אולי ב|בעיר|באזור|ליד) (.+)$/.exec(terms);
            const locationSuffix = /^(.*?) (?:באזור|בעיר|ליד) (.+)$/.exec(terms);
            if (everywhere) {
                city = "";
                terms = "";
            } else if (cityName) {
                city = cityName;
                terms = (" " + terms + " ").replace(" ב" + cityName + " ", " ").replace(" " + cityName + " ", " ").trim();
            } else if (locationSuffix || (product && locationOnly && /^(בעיר|באזור|ליד)$/.test(locationOnly[1]))) {
                const unknownCity = locationSuffix?.[2] || locationOnly[2];
                const requestedProduct = (locationSuffix?.[1] || "").split(" ").filter(word => !filler.has(word)).join(" ");
                if (requestedProduct) product = requestedProduct;
                lastMatches = [];
                shownMatches = 0;
                awaitingCity = true;
                message("לא מצאתי את ״" + unknownCity + "״ ברשימת הערים באתר. יש עיר אחרת שמתאימה, או שנחפש בכל הארץ?");
                return;
            } else if (awaitingCity && !/^(אני |מחפש|מחפשת|צריך|צריכה|רוצה|גמח )/.test(terms)) {
                message("לא מצאתי את ״" + question + "״ ברשימת הערים באתר. אפשר לכתוב עיר אחרת או ״כל הארץ״.");
                return;
            }

            const nextProduct = terms.replace(/^(ו?מה עם|מה לגבי|יש גם|ואולי|ואם יש|ו?איפה יש)(?: |$)/, "").split(" ").filter(word => !filler.has(word)).join(" ");
            if (nextProduct) product = nextProduct;
            if (!product) {
                message(city ? "יש לי את העיר. איזה פריט תרצו לשאול?" : "מה תרצו לשאול? אפשר גם לכתוב שם של גמ״ח.");
                return;
            }
            if (!city && !everywhere && !directory.some(group => searchable(group.name) === searchable(product))) {
                awaitingCity = true;
                lastMatches = [];
                shownMatches = 0;
                message("באיזו עיר לחפש ״" + product + "״? אם לא משנה איפה, כתבו ״כל הארץ״.");
                return;
            }
            awaitingCity = false;
            lastMatches = [];
            shownMatches = 0;

            const matchingCity = group => !city || clean(group.city) === city;
            const term = searchable(product);
            const namedGroups = directory.filter(group => matchingCity(group) && searchable(group.name).includes(term));
            const variants = [...new Set([product, product.replaceAll("כיסא", "כסא"), product.replaceAll("כסא", "כיסא")])];
            const findGroups = query => apiRequest("/gmachs?" + new URLSearchParams({ query }));
            const lists = await Promise.all(variants.map(findGroups));
            let candidates = lists.flat().filter(matchingCity);
            const words = term.split(" ");
            if (!candidates.length && words.length > 1) {
                const longestWord = variants.flatMap(variant => variant.split(" ")).reduce((longest, word) =>
                    word.length > longest.length ? word : longest);
                candidates = (await findGroups(longestWord)).filter(matchingCity);
            }
            const groups = new Map(candidates.map(group => [group.id, group]));
            if (currentRequest !== requestNumber) return;

            const checks = await Promise.allSettled([...groups.values()].slice(0, 12).map(async group => {
                const details = await apiRequest("/gmachs/" + encodeURIComponent(group.id));
                const item = details.items.find(item => {
                    const description = searchable(item.name + " " + (item.description || ""));
                    return item.isActive && item.availableCopies > 0 && words.every(word => description.includes(word));
                });
                return { group, item };
            }));
            if (currentRequest !== requestNumber) return;
            const matches = checks.filter(check => check.status === "fulfilled" && check.value.item)
                .map(check => check.value);
            if (matches.length) {
                lastMatches = matches;
                message((wantMore ? "הרחבתי את החיפוש לכל הארץ. " : "") +
                    (matches.length === 1 ? "מצאתי אפשרות שמתאימה" : "מצאתי כמה אפשרויות שמתאימות") +
                    (city ? " ב" + city : "") + ". לחצו על אחת מהן לפרטים.");
                showNextMatches();
            } else if (namedGroups.length) {
                message("מצאתי גמ״חים שהשם שלהם מתאים לחיפוש.");
                showResults(namedGroups.slice(0, 5).map(group => ({ group })));
            } else if (checks.some(check => check.status === "rejected")) {
                message("לא הצלחתי לבדוק את כל התוצאות כרגע. אפשר לנסות שוב.");
            } else {
                message(city
                    ? "לא מצאתי כרגע ״" + product + "״ זמין ב" + city + ". לחפש בעיר אחרת? אפשר גם לכתוב ״כל הארץ״."
                    : "לא מצאתי פריט זמין שתואם לחיפוש. נסו לכתוב את שם הפריט בקצרה, למשל ״הליכון״.");
            }
        } catch {
            if (currentRequest === requestNumber) message("לא הצלחתי לטעון את נתוני האתר כרגע. אפשר לנסות שוב.");
        } finally {
            if (currentRequest === requestNumber) setBusy(false);
        }
    }

    form.addEventListener("submit", event => {
        event.preventDefault();
        reply(input.value.trim());
    });

    reset();
})();
