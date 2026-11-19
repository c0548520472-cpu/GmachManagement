(() => {
    const launcher = document.getElementById("gmachGuideLauncher");
    const dialog = document.getElementById("gmachGuide");
    const log = document.getElementById("gmachGuideLog");
    const status = document.getElementById("gmachGuideStatus");
    const form = document.getElementById("gmachGuideForm");
    const input = document.getElementById("gmachGuideInput");
    const send = document.getElementById("gmachGuideSend");
    const filler = new Set("שלום היי אני אנחנו מחפש מחפשת מחפשים צריך צריכה צריכים רוצה רוצים אפשר האם יש לכם גמח איפה ניתן להשאיל השאלה לשאול למצוא פריט באזור בעיר של את לי תודה בבקשה דחוף היום מחר עכשיו מחפשת היכן בשביל עבור לקבל לקחת תמצאו תמצא מעוניין מעוניינת אולי במקרה ומה לגבי במקום עדיף דווקא רק משהו כזה אותו באזורכם קרוב אילו איזה איזו סוגי כזה ניתן אם מה מקום מקומות שמשאילים שמשאיל משאילים לשימוש ליד סביבות נמצא נמצאת באזורי עדיין אצלכם אצל להשאלה להשאיל מחפשים זקוק זקוקה זקוקים מחפש מחפשת צריך צריכה אפשרות ברשותכם מעוניינים ניתן זמין זמינה זמינים במלאי בכלל גם בשבילי בבקשת".split(" "));
    // The same aliases normalize questions and catalog entries. Availability is checked on the original items.
    const aliases = [
        ["כסא גלגלים", /כיסא עם גלגלים|כסא עם גלגלים|כיסאות גלגלים|כסאות גלגלים|כיסא גלגלי?ים|כסא גלגלי?ים/gu],
        ["הליכון", /הליכונים|מכשיר עזר להליכה|מתקן עזר להליכה|משהו שיעזור לי ללכת/gu],
        ["קביים", /קבי הליכה|קבים/gu],
        ["מקדחה", /מקדחות|מכשיר לקידוח|משהו לקדוח איתו|כלי לקידוח/gu],
        ["מחשב נייד", /מחשבים ניידים|לפטופים|לפטופ|לאפטופ/gu],
        ["אמבטיית תינוק", /אמבטיה לתינוק(?:ות|ת)?|אמבטיות לתינוק(?:ות)?|אמבטיית תינוקות/gu],
        ["מטען נייד", /מטענים ניידים|סוללה ניידת|פאוור בנק|power bank(?:s)?/gu],
        ["מקרן", /מקרנים|פרוג קטור|פרוג׳קטור|פרוגקטור|projector/gu],
        ["מכונת שטיפה בלחץ", /מכונות שטיפה בלחץ|גרניק|מכונת לחץ מים/gu],
        ["מד לחץ דם", /מדי לחץ דם|מכשיר למדידת לחץ דם|מכשיר לבדיקת לחץ דם/gu]
    ];
    // Category vocabulary and conversational intent are derived only from the site's catalog.
    const topics = [
        {name:"ציוד רפואי ובריאות", words:["רפואי","רפואה","בריאות","שיקום","ניידות","הליכה","חולה","חולים","כיסא גלגלים","הליכון","קביים","מכשור רפואי"]},
        {name:"תינוקות וילדים", words:["תינוק","תינוקת","תינוקות","ילד","ילדים","לידה","עגלה","עריסה","צעצוע","משחק"]},
        {name:"אירועים ושמחות", words:["אירוע","אירועים","שמחה","שמחות","חתונה","שבת חתן","בר מצווה","מסיבה","כיסאות","כסאות","כסא","כיסא","ספסל","ספסלים","שולחן","שולחנות","כלים לאירוע"]},
        {name:"כלי עבודה ותחזוקה", words:["עבודה","שיפוץ","תיקון","לתקן","קידוח","לקדוח","מקדחה","כלים","סולם","מברגה","תחזוקה"]},
        {name:"ספרים ולימוד", words:["ספר","ספרים","לימוד","ללמוד","גמרא","סידור","מחזור","תורני"]},
        {name:"ציוד לבית ולמשפחה", words:["לבית","בית","מטבח","בישול","חשמל","מזרן","ריהוט","ניקיון"]},
        {name:"תחבורה ונסיעות", words:["נסיעה","נסיעות","טיול","טיולים","רכב","אופניים","מזוודה","תחבורה"]},
        {name:"ביגוד ואביזרים", words:["בגד","בגדים","ביגוד","לבוש","נעליים","חליפה","תחפושת"]}
    ];
    const contextAliases = [
        ["כיסא גלגלים", /לא יכול ללכת|קשה לי ללכת|מתקשה ללכת|צריך להתנייד|כסא לנכה/gu],
        ["מקדחה", /לעשות חור בקיר|לקדוח בקיר|צריך לקדוח|חורים בקיר/gu],
        ["סולם", /להגיע למקום גבוה|להגיע לתקרה|להחליף מנורה גבוהה/gu],
        ["מקרן", /להקרין סרט|להציג מצגת על קיר|להקרין מצגת/gu],
        ["עגלה", /לטייל עם התינוק|להסיע תינוק|להסיע את התינוק/gu]
    ];
    function normalizeWords(value) {
        return searchable(contextAliases.reduce((s,[name,pattern]) => s.replace(pattern,name), String(value || "")));
    }
    function matchesTerm(haystack, query) {
        const text = " " + normalizeWords(haystack) + " ";
        const q = normalizeWords(query);
        if (!q) return false;
        if (text.includes(" " + q + " ")) return true;
        const words = q.split(" ").filter(w => w.length > 1);
        return words.length > 0 && words.every(word => text.includes(" " + word) || text.includes(" " + word.slice(0,Math.max(2,word.length-1))));
    }
    function relevantTopic(query) {
        const q = normalizeWords(query);
        const tokens = q.split(" ").filter(Boolean);
        const normalizeToken = token => token.replace(/^[והבכלמש]+(?=.{3})/u,"").replace(/ים$|ות$/u,"");
        const queryTokens = tokens.map(normalizeToken);
        let best = null, bestScore = 0;
        for (const topic of topics) {
            let score = 0;
            for (const word of [topic.name, ...topic.words]) {
                const normalized = normalizeWords(word);
                if (q.includes(normalized) || normalized.includes(q) && q.length >= 4) score += 4;
                else if (normalized.split(" ").some(token => queryTokens.includes(normalizeToken(token)))) score += 2;
            }
            if (score > bestScore) { best = topic; bestScore = score; }
        }
        return bestScore >= 2 ? best : null;
    }
    function matchesCategory(group, topic) {
        if (!topic) return false;
        // A category match must come from the owner's confirmed classification,
        // not from generic words inside unrelated gmach names.
        const category = normalizeWords(group.type || group.category || "");
        return category === normalizeWords(topic.name);
    }
    function isCategoryRequest(terms, topic) {
        if (!topic) return false;
        const query = normalizeWords(terms);
        const explicit = /(?:קטגוריה|קטגוריות|נושא|נושאים|סוגי גמחים|גמחים ל|גמחים של)/u.test(query);
        const categoryName = normalizeWords(topic.name);
        const reversed = categoryName.split(" ").reverse().join(" ");
        return explicit || query === categoryName || query === reversed ||
            (query.split(" ").length <= 3 && matchesTerm(categoryName, query) && !/ספסל|כסא|כיסא|שולחן/u.test(query));
    }
    function scoreMatch(group,item,query,topic) {
        const name=normalizeWords(item?.name || "");
        const desc=normalizeWords(item?.description || "");
        const groupName=normalizeWords(group.name);
        const category=normalizeWords(group.type || "");
        let score=0;
        if(matchesTerm(name,query))score+=12;
        if(matchesTerm(desc,query))score+=5;
        if(matchesTerm(groupName,query))score+=4;
        if(topic && category===normalizeWords(topic.name))score+=2;
        if(item?.availableCopies>0)score+=3;
        return score;
    }
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
        return aliases.reduce((text, [name, pattern]) => text.replace(pattern, name), clean(value));
    }

    function productTerms(terms) {
        return terms
            .replace(/^(ו?מה עם|מה לגבי|יש גם|ואולי|ואם יש|ו?איפה יש)(?: |$)/, "")
            .replace(/(?:^| )(?:רציתי לדעת(?: אם)?|רק רציתי לשאול|אתם יודעים(?: אם)?|מישהו יודע(?: אם)?|יש מצב(?: שיש)?|אשמח לדעת(?: אם)?|אני מחפש(?:ת)?|אפשר לעזור לי למצוא|האם אתם יכולים למצוא לי|להשאיל לי|שאפשר להשאיל|גמח שמשאיל|לכמה ימים|ליום אחד|לזמן קצר)(?= |$)/g, " ")
            .split(" ").filter(word => word && !filler.has(word)).join(" ");
    }

    function includesPhrase(text, phrase) {
        return (" " + text + " ").includes(" " + phrase + " ");
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
        message("היי! מה מחפשים היום? אפשר לחפש לפי שם גמ״ח, קטגוריה או פריט. למשל ״ספסלים וכיסאות לאירוע״ או ״גמ״חים לשמחות ואירועים״.");
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
            detail.textContent = item
                ? item.name + (item.availableCopies > 0
                    ? " · עותקים זמינים: " + item.availableCopies
                    : " · כרגע מושאל" + (item.expectedReturnDate
                        ? " · צפי החזרה: " + new Date(item.expectedReturnDate).toLocaleDateString("he-IL") + " (משוער)"
                        : " · אין תאריך חזרה מאומת"))
                : "פתיחת דף הגמ״ח";
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
        let terms = clean(question).replace(/^(?:היי|שלום|אהלן) (?!רב(?:ה)?$)/, "").replace(/ תודה(?: רבה)?$/, "");
        if (/^(שלום|היי|הי|אהלן|בוקר טוב|ערב טוב|מה נשמע)$/.test(terms)) {
            message(product ? "היי! אפשר להמשיך עם החיפוש, או לכתוב שם של פריט אחר." : "היי! בשמחה אעזור למצוא גמ״ח. מה מחפשים?");
            return;
        }
        if (/^(תודה( רבה| לך| לכם| על העזרה| עזרת לי)?|תודה רבה על העזרה|מעולה|מצוין|סבבה תודה|אחלה תודה)$/.test(terms)) {
            message("בשמחה! אפשר לעזור בעוד משהו?");
            return;
        }
        if (product && /^(עיר אחרת|בעיר אחרת|לחפש בעיר אחרת)$/.test(terms)) {
            awaitingCity = true;
            message("בשמחה, באיזו עיר לחפש?");
            return;
        }
        if (/^(כן )?(משהו אחר|פריט אחר|חיפוש חדש|ננסה משהו אחר|לחפש משהו אחר)$/.test(terms)) {
            product = "";
            awaitingCity = false;
            lastMatches = [];
            shownMatches = 0;
            message("בטח, בואו ננסה משהו אחר. איזה פריט לחפש?");
            return;
        }
        if (/^(איך (אפשר )?(משאילים|להשאיל|לשאול|מבקשים|לבקש|מזמינים|להזמין|אוספים|לאסוף)( את)?( הפריט| פריט| זה| אותו)?|איך (זה עובד|יוצרים קשר|אפשר ליצור קשר|מדברים עם הגמח)|מה עושים עכשיו)$/.test(terms)) {
            message("פתחו את דף הגמ״ח ובחרו ״השאלה״ ליד הפריט. אפשר גם לפתוח את הצ׳אט של הגמ״ח כדי לשאול את הבעלים.");
            return;
        }
        if (/^(כמה( עותקים| יחידות)?( יש| זמינים| זמינות| נשארו| יש במלאי)?|יש במלאי|זה( עדיין)? זמין|יש זמין|עוד יש במלאי)$/.test(terms) && lastMatches.length) {
            message("מספר העותקים הזמינים מופיע ליד כל פריט בתוצאות.");
            showResults(lastMatches.slice(0, 5));
            return;
        }
        const wantMore = /^(יש עוד( אפשרויות| תוצאות| מקומות)?|עוד( אפשרויות| תוצאות)?|תראה לי עוד|איפה עוד( יש| אפשר למצוא)?|יש מקום נוסף)$/.test(terms);
        if (wantMore && shownMatches < lastMatches.length) {
            message("כן, הנה עוד אפשרויות שמצאתי.");
            showNextMatches();
            return;
        }
        if (wantMore && !city) {
            message(product ? "אלה האפשרויות שמצאתי כרגע. אולי ננסה שם אחר לפריט? אפשר גם לחפש משהו אחר." : "מה תרצו לחפש? כתבו שם של פריט או גמ״ח.");
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
            const everywhere = wantMore || /^(ב?כל הארץ|ב?כל עיר|לא משנה( לי)?( העיר| איפה)?|אין העדפה|איפה שיש|(?:תחפש|אפשר לחפש|חפש)(?: גם)? בכל הארץ|מבחינתי בכל מקום|איפה שבא לך)$/.test(terms);
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
                const requestedProduct = productTerms(searchable(locationSuffix?.[1] || ""));
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

            // A direct gmach-name request takes priority over product/category discovery.
            const requestedName = normalizeWords(terms)
                .replace(/^(?:תמצא(?:י)? לי|תראה(?:י)? לי|חפש(?:י)? לי|אני מחפש(?:ת)?|אני רוצה|איפה נמצא|איפה יש|יש לכם|יש|פרטים על|מידע על|מחפש(?:ת)? את|מחפש(?:ת)? גמח בשם|גמח בשם|גמח של|גמח|הגמח של|הגמח|שם הגמח)\s+/u, "")
                .replace(/^(?:שנקרא|בשם|של)\s+/u, "").trim();
            const groupNameMatches = directory.filter(group => {
                const groupName = normalizeWords(group.name).replace(/^גמח\s+/u, "");
                const queryName = requestedName.replace(/^גמח\s+/u, "");
                return queryName.length >= 2 && (groupName.includes(queryName) ||
                    (queryName.length >= 4 && matchesTerm(groupName, queryName)));
            }).sort((a,b) => {
                const nameA = normalizeWords(a.name), nameB = normalizeWords(b.name);
                return Number(nameB === requestedName) - Number(nameA === requestedName);
            });
            const explicitlyNamed = /(?:גמח|הגמח|בשם|שנקרא|פרטים על|מידע על)/u.test(normalizeWords(question));
            if (groupNameMatches.length && (explicitlyNamed || groupNameMatches.some(group => normalizeWords(group.name) === requestedName))) {
                const filtered = groupNameMatches.filter(group => !city || clean(group.city) === city);
                const results = filtered.length ? filtered : groupNameMatches;
                product = requestedName;
                awaitingCity = false;
                lastMatches = results.map(group => ({group}));
                shownMatches = 0;
                message("מצאתי " + results.length + " גמ״חים שמתאימים לשם שביקשת. לחצו על גמ״ח כדי לפתוח את העמוד שלו.");
                showNextMatches();
                return;
            }
            const requestedTopic = relevantTopic(terms);
            const nextProduct = productTerms(normalizeWords(terms));
            if (nextProduct) product = nextProduct;
            if (!product) {
                message(city ? "יש לי את העיר. איזה פריט תרצו לשאול?" : "מה תרצו לשאול? אפשר גם לכתוב שם של גמ״ח.");
                return;
            }
            awaitingCity = false;
            lastMatches = [];
            shownMatches = 0;
            const matchingCity = group => !city || clean(group.city) === city;
            const term = normalizeWords(product);
            const topic = requestedTopic || relevantTopic(product);
            const namedGroups = directory.filter(group => matchingCity(group) && matchesTerm(group.name,term));
            if (isCategoryRequest(terms, topic)) {
                const topicGroups = directory.filter(group => matchingCity(group) && matchesCategory(group,topic));
                if (topicGroups.length) {
                    lastMatches = topicGroups.map(group => ({group}));
                    message("מצאתי " + topicGroups.length + " גמ״חים בקטגוריה ״" + topic.name + "״. הנה הגמ״חים שמופיעים באתר:");
                    showNextMatches();
                    return;
                }
            }
            // Search each meaningful product separately, rather than searching for
            // the entire sentence (e.g. "ספסלים וכסאות") as a single product.
            const stop = new Set(["אני","רוצה","צריך","צריכה","מחפש","מחפשת","לי","את","של","עם","לאירוע","לאירועים","באזור","למצוא","חפש","אפשר","גמח","גמחים"]);
            const productTermsList = term.split(/\s+|[,،]/u)
                .map(word => word.replace(/^[והבכלמש]+(?=.{3})/u,""))
                .filter(word => word.length >= 3 && !stop.has(word));
            const variants = [...new Set([term, ...productTermsList,
                ...productTermsList.map(word => word.replace("כסא","כיסא")),
                ...productTermsList.map(word => word.replace("כיסא","כסא"))])].filter(Boolean);
            const findGroups = query => apiRequest("/gmachs?" + new URLSearchParams({query}));
            const lists = await Promise.allSettled(variants.slice(0,8).map(findGroups));
            const candidates = lists.filter(result => result.status === "fulfilled").flatMap(result => result.value).filter(matchingCity);
            const groups = new Map([...candidates,...directory.filter(group => matchingCity(group) &&
                (matchesTerm(group.name,term) || productTermsList.some(word => matchesTerm(group.name,word))))].map(group => [group.id,group]));
            if (currentRequest !== requestNumber) return;
            const queryTokens = productTermsList.length ? productTermsList : [term];
            const relevantItemScore = (group,item) => {
                const name = normalizeWords(item.name || "");
                const description = normalizeWords(item.description || "");
                let score = 0;
                for (const word of queryTokens) {
                    const root = word.replace(/ים$|ות$/u,"");
                    if (root.length < 2) continue;
                    if (name.includes(root)) score += 10;
                    else if (description.includes(root)) score += 4;
                }
                return score;
            };
            const checks = await Promise.allSettled([...groups.values()].slice(0, 12).map(async group => {
                const details = await apiRequest("/gmachs/" + encodeURIComponent(group.id));
                const items = (details.items || []).filter(item => item.isActive !== false);
                const ranked = items.map(item => ({item, score:relevantItemScore(group,item)})).sort((a,b)=>b.score-a.score);
                const best = ranked[0];
                const item = best && best.score >= 4 ? best.item : null;
                return { group, item };
            }));
            if (currentRequest !== requestNumber) return;
            const matches = checks.filter(check => check.status === "fulfilled" && check.value.item)
                .map(check => check.value)
                .sort((a,b) => relevantItemScore(b.group,b.item) - relevantItemScore(a.group,a.item));
            if (matches.length) {
                const availableCount = matches.filter(match => match.item.availableCopies > 0).length;
                lastMatches = matches;
                message((wantMore ? "הרחבתי את החיפוש לכל הארץ. " : "") +
                    (availableCount ? (availableCount === 1 ? "מצאתי אפשרות זמינה" : "מצאתי כמה אפשרויות זמינות") : "מצאתי את הפריט, אבל כרגע הוא מושאל") +
                    (city ? " ב" + city : "") + ". לחצו על אחת מהן לפרטים. תאריך החזרה מוצג רק אם קיים מידע מאומת באתר.");
                showNextMatches();
            } else if (namedGroups.length) {
                message("מצאתי גמ״חים שהשם שלהם מתאים לחיפוש.");
                showResults(namedGroups.slice(0, 5).map(group => ({ group })));
            } else if (checks.some(check => check.status === "rejected")) {
                message("לא הצלחתי לבדוק את כל התוצאות כרגע. אפשר לנסות שוב.");
            } else {
                message(city
                    ? "יואו, איזה מבאס. כרגע לא מצאתי ״" + product + "״ זמין ב" + city +
                        ". בואו ננסה בדרך אחרת. לחפש בעיר אחרת או בכל הארץ? אפשר גם לכתוב ״משהו אחר״."
                    : "יואו, איזה מבאס. לא מצאתי ״" + product +
                        "״ זמין כרגע. אולי ננסה שם אחר לאותו פריט? אפשר לעזור עם פריט אחר? אם כן, כתבו ״משהו אחר״.");
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
