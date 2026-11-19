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
        {name:"אירועים ושמחות", words:["אירוע","אירועים","שמחה","שמחות","חתונה","שבת חתן","בר מצווה","מסיבה","כיסאות","שולחנות","כלים לאירוע"]},
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
        const q=normalizeWords(query);
        return topics.find(t=>matchesTerm(t.name,q) || t.words.some(word=>matchesTerm(q,word) || matchesTerm(word,q)));
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
        if(topic && (category===normalizeWords(topic.name) || matchesTerm(groupName,topic.name) || matchesTerm(desc,topic.name)))score+=6;
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
        message("היי! מה מחפשים היום? אפשר לחפש פריט, נושא או גמ״ח לפי שם. למשל ״אני צריכה הליכון באזור ירושלים״ או ״חפש גמ״ח חסדי אברהם״.");
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
            const nextProduct = productTerms(normalizeWords(terms));
            if (nextProduct) product = nextProduct;
            if (!product) {
                message(city ? "יש לי את העיר. איזה פריט תרצו לשאול?" : "מה תרצו לשאול? אפשר גם לכתוב שם של גמ״ח.");
                return;
            }
            if (!city && !everywhere && !relevantTopic(product) && !directory.some(group => matchesTerm(group.name,product))) {
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
            const term = normalizeWords(product);
            const topic = relevantTopic(product);
            const namedGroups = directory.filter(group => matchingCity(group) && (matchesTerm(group.name,term) || (topic && normalizeWords(group.type)===normalizeWords(topic.name))));
            const variants = [...new Set([product, term, term.replaceAll("כסא", "כיסא"), ...(topic ? [topic.name, ...topic.words.slice(0,5)] : [])])];
            // A singular query may miss a catalog name written with another spelling or plural.
            const concept = aliases.find(([name]) => includesPhrase(term, name));
            if (concept) {
                const [name] = concept;
                const firstWord = name.split(" ")[0];
                variants.push(name, firstWord.replace(/(?:ת|ה|ן)$/, ""));
            }
            const findGroups = query => apiRequest("/gmachs?" + new URLSearchParams({ query }));
            const lists = await Promise.all([...new Set(variants)].map(findGroups));
            let candidates = lists.flat().filter(matchingCity);
            const words = term.split(" ");
            if (!candidates.length && words.length > 1) {
                const longestWord = variants.flatMap(variant => variant.split(" ")).reduce((longest, word) =>
                    word.length > longest.length ? word : longest);
                candidates = (await findGroups(longestWord)).filter(matchingCity);
            }
            const groups = new Map([...candidates, ...directory.filter(group => matchingCity(group) && (matchesTerm(group.name,product) || (topic && normalizeWords(group.type)===normalizeWords(topic.name))))].map(group => [group.id, group]));
            if (currentRequest !== requestNumber) return;

            const checks = await Promise.allSettled([...groups.values()].slice(0, 12).map(async group => {
                const details = await apiRequest("/gmachs/" + encodeURIComponent(group.id));
                const items = (details.items || []).filter(item => item.isActive !== false);
                const ranked = items.map(item => ({item, score:scoreMatch(group,item,product,topic)})).sort((a,b)=>b.score-a.score);
                const best = ranked[0];
                const item = best && best.score >= (topic ? 6 : 5) ? best.item : null;
                return { group, item };
            }));
            if (currentRequest !== requestNumber) return;
            const matches = checks.filter(check => check.status === "fulfilled" && check.value.item)
                .map(check => check.value)
                .sort((a,b) => scoreMatch(b.group,b.item,product,topic) - scoreMatch(a.group,a.item,product,topic));
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
