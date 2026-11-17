
const API_BASE = "https://gmachmanagement-api.onrender.com/api";
const PAGE_SIZE = 6;
const normalizeCity = value => String(value ?? "").replaceAll("ירושליים","ירושלים");

const authSection = document.getElementById("authSection");
const dashboardSection = document.getElementById("dashboardSection");
const authForm = document.getElementById("authForm");
const registrationForm = document.getElementById("registrationForm");
const authMessage = document.getElementById("authMessage");
const itemsMessage = document.getElementById("itemsMessage");
const borrowMessage = document.getElementById("borrowMessage");
const borrowDialog = document.getElementById("borrowDialog");

let token = sessionStorage.getItem("gmachToken");
let currentPage = 1;
let currentItems = [];
let selectedCopyId = null;
let gmachSearchSequence = 0;
let gmachDetailsSequence = 0;
let activeChat = null;
let selectedChatGmach = null;
let openChatOnGmachLoad = null;
let chatTimer = null;
let approvalPending = false;
let openApprovalId = null;
let itemsRequestSequence = 0;
let authMode = "login";
let googleScript;
let googleMountSequence = 0;
let signedInName = "";
let profileNameToken = null;
async function refreshWelcomeName() {
    const activeToken = token;
    if (!activeToken || profileNameToken === activeToken) return;
    profileNameToken = activeToken;
    try {
        const profile = await apiRequest("/Auth/profile");
        if (token !== activeToken) return;
        signedInName = String(profile.name || "").trim().slice(0, 60);
        renderWelcomeName();
    } catch {
        if (token === activeToken) { signedInName = ""; renderWelcomeName(); }
    }
}
function renderWelcomeName() {
    document.getElementById("welcomeText").textContent =
        token ? (signedInName ? "היי, " + signedInName + " 👋" : "היי, ברוכים הבאים 👋") : "";
}

function showMessage(element, message, type = "") {
    element.className = `message ${type}`;
    if (type === "loading") element.replaceChildren(createDataLoader(message));
    else element.textContent = message;
}

function createDataLoader(message) {
    const status = document.createElement("span");
    status.className = "data-loading";
    status.setAttribute("role", "status");

    const motion = document.createElement("span");
    motion.className = "pac-loader";
    motion.setAttribute("aria-hidden", "true");

    const trail = document.createElement("span");
    trail.className = "pac-pellets";
    trail.append(document.createElement("i"), document.createElement("i"), document.createElement("i"));

    const mouth = document.createElement("span");
    mouth.className = "pac-mouth";
    motion.append(trail, mouth);

    const label = document.createElement("span");
    label.textContent = message;
    status.append(motion, label);
    return status;
}

function showDashboard() {
    showHome();
}

function showLogin() {
    showPage("login");
}

function logout() {
    sessionStorage.removeItem("gmachToken");
    token = null;
    signedInName = "";
    profileNameToken = null;
    currentItems = [];
    clearPublicData();
    document.getElementById("welcomeText").textContent = "";
    document.getElementById("accountMenu").open=false;
    showHome();
}

// Only cache public catalog endpoints. Never cache account or owner data.
const publicRequestCache = new Map();
const publicPendingRequests = new Map();
const PUBLIC_CACHE_MS = 60000;
const DIRECTORY_STORAGE_KEY = "gmachPublicDirectory";
const COOKIE_PREFERENCE_KEY = "gmachCookiePreference.v1";
let cookiePreference = null;
try {
    const saved = localStorage.getItem(COOKIE_PREFERENCE_KEY);
    if (["all", "partial", "none"].includes(saved)) cookiePreference = saved;
} catch { /* The preference can still be used for this page. */ }

const cookieNotice = document.getElementById("cookieNotice");
const cookieDetails = document.getElementById("cookieDetails");
const cookieCustomize = document.getElementById("cookieCustomize");
const directoryCacheChoice = document.getElementById("directoryCacheChoice");

function allowsDirectoryStorage() {
    return cookiePreference === "all" || cookiePreference === "partial";
}

function openCookieNotice(moveFocus = false) {
    cookieDetails.hidden = true;
    cookieCustomize.setAttribute("aria-expanded", "false");
    directoryCacheChoice.checked = allowsDirectoryStorage();
    cookieNotice.hidden = false;
    if (moveFocus) document.getElementById("cookieTitle").focus();
}

function saveCookiePreference(choice) {
    cookiePreference = choice;
    try { localStorage.setItem(COOKIE_PREFERENCE_KEY, choice); } catch { /* Storage is optional. */ }
    if (!allowsDirectoryStorage()) {
        try { sessionStorage.removeItem(DIRECTORY_STORAGE_KEY); } catch { /* Storage is optional. */ }
    } else {
        const currentDirectory = publicRequestCache.get("/gmachs");
        if (currentDirectory?.expiresAt > Date.now()) storeDirectorySnapshot(currentDirectory.value);
    }
    const restoreFocus = cookieNotice.contains(document.activeElement);
    cookieNotice.hidden = true;
    if (restoreFocus) document.getElementById("cookieSettings").focus();
}

document.getElementById("cookieSettings").addEventListener("click", () => openCookieNotice(true));
document.getElementById("cookieAccept").addEventListener("click", () => saveCookiePreference("all"));
document.getElementById("cookieReject").addEventListener("click", () => saveCookiePreference("none"));
cookieCustomize.addEventListener("click", () => {
    cookieDetails.hidden = false;
    cookieCustomize.setAttribute("aria-expanded", "true");
    directoryCacheChoice.focus();
});
document.getElementById("cookieSave").addEventListener("click", () => {
    saveCookiePreference(directoryCacheChoice.checked ? "partial" : "none");
});
if (!allowsDirectoryStorage()) {
    try { sessionStorage.removeItem(DIRECTORY_STORAGE_KEY); } catch { /* Storage is optional. */ }
}
if (cookiePreference === null) openCookieNotice();

const DIRECTORY_SNAPSHOT_MS = 5 * 60 * 1000;
function readDirectorySnapshot() {
    if (!allowsDirectoryStorage()) return null;
    try {
        const snapshot = JSON.parse(sessionStorage.getItem(DIRECTORY_STORAGE_KEY));
        if (snapshot?.expiresAt > Date.now() && Array.isArray(snapshot.groups)) return snapshot.groups;
    } catch { /* A directory request still works when storage is unavailable. */ }
    return null;
}
function storeDirectorySnapshot(groups) {
    if (!allowsDirectoryStorage()) return;
    try {
        sessionStorage.setItem(DIRECTORY_STORAGE_KEY, JSON.stringify({
            groups, expiresAt: Date.now() + DIRECTORY_SNAPSHOT_MS
        }));
    } catch { /* Storage is optional. */ }
}
function clearPublicData() {
    publicRequestCache.clear();
    publicPendingRequests.clear();
    try { sessionStorage.removeItem(DIRECTORY_STORAGE_KEY); } catch { /* Storage is optional. */ }
}
function publicCacheKey(path, options) {
    if (options.method && options.method.toUpperCase() !== "GET") return null;
    return /^\/(?:gmachs(?:\?|$|\/\d+$)|categories(?:\?|$)|items\?)/.test(path) ? path : null;
}
async function apiRequest(path, options = {}) {
    const cacheKey = publicCacheKey(path, options);
    if (!cacheKey) {
        if (options.method && options.method.toUpperCase() !== "GET") {
            clearPublicData();
        }
        return requestFromServer(path, options);
    }
    const cached = publicRequestCache.get(cacheKey);
    if (cached && cached.expiresAt > Date.now()) return cached.value;
    const pending = publicPendingRequests.get(cacheKey);
    if (pending) return pending;
    const request = requestFromServer(path, options).then(result => {
        if (publicPendingRequests.get(cacheKey) === request) {
            publicRequestCache.set(cacheKey, {value: result, expiresAt: Date.now() + PUBLIC_CACHE_MS});
            if (cacheKey === "/gmachs") storeDirectorySnapshot(result);
        }
        return result;
    }).finally(() => {
        if (publicPendingRequests.get(cacheKey) === request) publicPendingRequests.delete(cacheKey);
    });
    publicPendingRequests.set(cacheKey, request);
    return request;
}
async function requestFromServer(path, options = {}) {
    const headers = new Headers(options.headers || {});

    // Public directory endpoints must work without login or an expired saved token.
    const publicGmachGet = (!options.method || options.method.toUpperCase() === "GET") && /^\/gmachs(?:\?|$|\/\d+(?:\?|$))/.test(path);
    if (token && !publicGmachGet) {
        headers.set("Authorization", `Bearer ${token}`);
    }

    if (options.body) {
        headers.set("Content-Type", "application/json");
    }

    let response;

    try {
        response = await fetch(`${API_BASE}${path}`, {
            ...options,
            headers
        });
    } catch (error) {
        console.error("API connection failed", {
            endpoint: path,
            origin: window.location.origin,
            cause: error instanceof Error ? error.message : String(error)
        });
        throw new Error(
            "לא ניתן ליצור קשר עם שרת האתר. יש לבדוק את החיבור לאינטרנט ולנסות שוב. אם הבעיה נמשכת, יש לפתוח את כלי המפתחים (F12) ולבדוק את לשונית Network."
        );
    }

    if (response.status === 401 && token && !path.toLowerCase().includes("/auth/")) {
        logout();
        throw new Error(
            "יש להתחבר מחדש למערכת."
        );
    }

    if (!response.ok) {
        let message = `שגיאת שרת (${response.status})`;

        const text = await response.text();

        try {
            const data = JSON.parse(text);
            message = data.message || (data.errors ? Object.values(data.errors).flat().join(" ") : null) || data.title || message;
        } catch {
            if (text && text.length < 250) {
                message = text;
            }
        }

        if (response.status === 409 && path.toLowerCase() === "/loans") {
            throw new Error("העותק נתפס על ידי משתמש אחר. יש לרענן את הרשימה ולנסות שוב.");
        }

        throw new Error(message);
    }

    if (response.status === 204) {
        return null;
    }

    const result = await response.json();
    return result;
}

document.getElementById("forgotPasswordBtn").addEventListener("click",()=>{
 const notice=document.getElementById("forgotPasswordNotice");
 notice.hidden=false;
});

// התחברות
registrationForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    const name = document.getElementById("fullName").value.trim();
    const email = document.getElementById("registrationEmail").value.trim();
    const password = document.getElementById("registrationPassword").value;

    if (!name) {
        showMessage(authMessage, "יש להזין שם מלא כדי להירשם.", "error");
        document.getElementById("fullName").focus();
        return;
    }

    if (password.length < 8 || password.length > 128 || !hasRegistrationPasswordMix(password)) {
        showMessage(authMessage, password.length > 128
            ? "הסיסמה ארוכה מדי."
            : "הסיסמה חייבת לכלול לפחות 8 תווים, ובהם אות וספרה.", "error");
        document.getElementById("registrationPassword").focus();
        return;
    }
    showMessage(authMessage, "מבצע הרשמה...");

    try {
        await apiRequest("/Auth/register", {
            method: "POST",
            body: JSON.stringify({ name, email, password, isGmachOwner: document.getElementById("registerOwner").checked, gmachName: document.getElementById("gmachName").value.trim(), city: normalizeCity(document.getElementById("gmachCity").value.trim()) })
        });

        switchAuthMode("login");


        showMessage(
            authMessage,
            "ההרשמה הצליחה! כעת אפשר להתחבר.",
            "success"
        );
    } catch (error) {
        showMessage(authMessage, error.message, "error");
    }
});

authForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    const email = document.getElementById("email").value.trim();
    const password = document.getElementById("password").value;

    showMessage(authMessage, "מתחבר...");

    try {
        const result = await apiRequest("/Auth/login", {
            method: "POST",
            body: JSON.stringify({ email, password })
        });

        if (!result.accessToken) {
            throw new Error("השרת לא החזיר אסימון התחברות.");
        }

        token = result.accessToken;
        sessionStorage.setItem("gmachToken", token);
        signedInName = "";
        profileNameToken = null;
        showMessage(authMessage, "");
        void refreshWelcomeName();
        showDashboard();
    } catch (error) {
        showMessage(authMessage, error.message, "error");
    }
});

function hasRegistrationPasswordMix(password) {
    return /\p{L}/u.test(password) && /\p{Nd}/u.test(password);
}

function switchAuthMode(mode) {
    const isRegistration = mode === "register";
    if (authMode !== mode) {
        const password = document.getElementById(authMode === "register" ? "registrationPassword" : "password");
        const toggle = document.getElementById(authMode === "register" ? "registrationShowPassword" : "showPassword");
        password.value = "";
        password.type = "password";
        toggle.checked = false;
    }
    authMode = mode;
    authForm.hidden = isRegistration;
    authForm.inert = isRegistration;
    registrationForm.hidden = !isRegistration;
    registrationForm.inert = !isRegistration;
    document.querySelector("#authSection .tabs").hidden = false;
    document.getElementById("loginTab").classList.toggle("active", !isRegistration);
    document.getElementById("registerTab").classList.toggle("active", isRegistration);
    document.getElementById("forgotPasswordBtn").hidden = isRegistration;
    document.getElementById("forgotPasswordNotice").hidden = true;
    showMessage(authMessage, "");
}

document.getElementById("loginTab").addEventListener(
    "click", () => switchAuthMode("login")
);

document.getElementById("registerTab").addEventListener(
    "click", () => switchAuthMode("register")
);

document.getElementById("registrationPassword").addEventListener("input", () => showMessage(authMessage, ""));

document.getElementById("logoutBtn").addEventListener(
    "click", logout
);

// טעינת פריטים מהשרת
async function loadItems() {
    const grid = document.getElementById("itemsGrid");
    const search = document.getElementById("searchInput").value.trim();
    const availableOnly =
        document.getElementById("availableOnly").checked;

    const params = new URLSearchParams({
        page: currentPage,
        pageSize: PAGE_SIZE,
        availableOnly
    });

    if (search) {
        params.set("search", search);
    }
    const category = document.getElementById("categoryFilter").value;
    if (category) params.set("categoryId", category);

    const requestSequence = ++itemsRequestSequence;
    const requestPath = `/items?${params.toString()}`;
    const cachedItems = publicRequestCache.get(requestPath);
    if (cachedItems && cachedItems.expiresAt > Date.now()) {
        renderItems(cachedItems.value);
        showMessage(itemsMessage, "");
    } else if (!currentItems.length) {
        showMessage(itemsMessage, "טוען פריטים...", "loading");
    }

    try {
        const items = await apiRequest(
            requestPath
        );
        if (requestSequence !== itemsRequestSequence) return;
        currentItems = items;
        renderItems(items);

        showMessage(
            itemsMessage,
            items.length === 0 ? "לא נמצאו פריטים." : ""
        );

        document.getElementById("pageLabel").textContent =
            `עמוד ${currentPage}`;

        document.getElementById("prevPage").disabled =
            currentPage === 1;

        document.getElementById("nextPage").disabled =
            items.length < PAGE_SIZE;

    } catch (error) {
        if (requestSequence === itemsRequestSequence) showMessage(itemsMessage, error.message, "error");
    }
}

function renderItems(items) {
    const grid = document.getElementById("itemsGrid");
    grid.replaceChildren();

    for (const item of items) {
        const available =
            item.isActive && item.availableCopies > 0;

        const card = document.createElement("article");
        card.className = "item-card";

        const title = document.createElement("h3");
        title.textContent = item.name;

        const description = document.createElement("p");
        description.className = "muted";
        description.textContent =
            item.description || "אין תיאור לפריט";

        const status = document.createElement("span");
        status.className =
            `status ${available ? "available" : "unavailable"}`;
        status.textContent = available
            ? `${item.availableCopies} עותקים זמינים`
            : "לא זמין";

        const details = document.createElement("p");
        details.className = "muted";
        details.textContent =
            `סך הכול עותקים: ${item.totalCopies}`;

        const borrowButton = document.createElement("button");
        borrowButton.className = "primary";
        borrowButton.textContent = "השאלת פריט";
        borrowButton.disabled = !available;


        borrowButton.addEventListener("click", () => {
            openBorrowDialog(item);
        });

        card.append(
            title,
            description,
            status,
            details,
            borrowButton
        );

        grid.appendChild(card);
    }
}

// חיפוש ודפדוף
document.getElementById("searchBtn").addEventListener(
    "click", () => {
        currentPage = 1;
        loadItems();
    }
);

document.getElementById("searchInput").addEventListener(
    "keydown", (event) => {
        if (event.key === "Enter") {
            currentPage = 1;
            loadItems();
        }
    }
);

document.getElementById("categoryFilter").addEventListener("change", () => { currentPage = 1; loadItems(); });
document.getElementById("availableOnly").addEventListener(
    "change", () => {
        currentPage = 1;
        loadItems();
    }
);

document.getElementById("prevPage").addEventListener(
    "click", () => {
        if (currentPage > 1) {
            currentPage--;
            loadItems();
        }
    }
);

document.getElementById("nextPage").addEventListener(
    "click", () => {
        if (currentItems.length === PAGE_SIZE) {
            currentPage++;
            loadItems();
        }
    }
);

// בחירת עותק זמין
async function openBorrowDialog(item) {
    showMessage(itemsMessage, "בודק עותקים זמינים...");

    try {
        if (!item.availableCopies) {
            showMessage(
                itemsMessage,
                "אין כרגע עותק זמין להשאלה.",
                "error"
            );
            await loadItems();
            return;
        }

        selectedCopyId = item.id;

        document.getElementById("borrowItemName").textContent =
            item.name;

        document.getElementById("borrowForm").reset();

        showMessage(borrowMessage, "");
        showMessage(itemsMessage, "");
        borrowDialog.showModal();

    } catch (error) {
        showMessage(itemsMessage, error.message, "error");
    }
}

// ביצוע השאלה
document.getElementById("borrowForm").addEventListener(
    "submit", async (event) => {
        event.preventDefault();

        if (selectedCopyId === null) {
            return;
        }

        const applicantName = document.getElementById("requestName").value.trim();
        const phone = document.getElementById("requestPhone").value.trim();
        const email = document.getElementById("requestEmail").value.trim();
        if (!phone && !email) { showMessage(borrowMessage, "יש להזין טלפון או אימייל.", "error"); return; }

        const submitButton =
            document.querySelector(
                '#borrowForm button[type="submit"]'
            );

        submitButton.disabled = true;
        showMessage(borrowMessage, "שולח בקשה...");

        try {
            await apiRequest("/borrow-requests", {
                method: "POST",
                body: JSON.stringify({
                    itemId: selectedCopyId,
                    applicantName,
                    phone: phone || null,
                    email: email || null
                })
            });

            borrowDialog.close();
            selectedCopyId = null;

            document.getElementById("borrowSuccessDialog").showModal();

        } catch (error) {
            showMessage(borrowMessage, error.message, "error");

            if (error.message.includes("העותק נתפס") || error.message.includes("אינו זמין")) {
                await loadItems();
            }
        } finally {
            submitButton.disabled = false;
        }
    }
);

document.getElementById("cancelBorrow").addEventListener(
    "click", () => borrowDialog.close()
);

borrowDialog.addEventListener("close", () => {
    selectedCopyId = null;
});

// פתיחת המערכת
if (token) {
    showDashboard();
} else {
    showHome();
}


// Personal borrowing / lending
// Shared client-side search for item name or description in each management view.
function matchesItemSearch(query, ...fields) {
    const normalized = query.trim().toLocaleLowerCase("he");
    return !normalized || fields.some(field => String(field ?? "").toLocaleLowerCase("he").includes(normalized));
}
function filterItemCards(containerId, query) {
    const container = document.getElementById(containerId);
    for (const card of container.querySelectorAll("[data-item-search]")) {
        card.hidden = !matchesItemSearch(query, card.dataset.itemSearch);
    }
}
function applyPersonalItemSearch() {
    const query = document.getElementById("personalItemSearch").value;
    filterItemCards("openPersonalList", query);
    filterItemCards("personalList", query);
}
function applyAdminItemSearch() {
    const query = document.getElementById("adminItemSearch").value;
    filterItemCards("adminItems", query);
    filterItemCards("removedAdminItems", query);
}
document.getElementById("personalItemSearch").addEventListener("input", applyPersonalItemSearch);
document.getElementById("adminItemSearch").addEventListener("input", applyAdminItemSearch);

async function loadPersonalTransactions() {
    const historyList = document.getElementById("personalList");
    const openList = document.getElementById("openPersonalList");
    const message = document.getElementById("personalMessage");
    const openMessage = document.getElementById("openPersonalMessage");
    historyList.replaceChildren();
    openList.replaceChildren();
    try {
        const entries = await apiRequest("/personal-transactions");
        const open = entries.filter(entry => !entry.isCompleted);
        const completed = entries.filter(entry => entry.isCompleted);
        showMessage(openMessage, open.length ? "" : "אין השאלות אישיות פתוחות.");
        showMessage(message, "");
        for (const entry of entries) {
            const row = document.createElement("article");
            row.className = "item-card";
            row.dataset.itemSearch = entry.itemDescription || "";
            const title = document.createElement("h3");
            title.textContent = entry.itemDescription || "פריט";
            const details = document.createElement("p");
            details.textContent = (entry.type === 1 ? "שאלתי מ: " : "השאלתי ל: ") + entry.otherPartyName;
            const status = document.createElement("p");
            status.textContent = entry.isCompleted ? "הוחזר" : "ממתין להחזרה";
            if (entry.amount != null) details.textContent += " | סכום: " + entry.amount;
            row.append(title, details, status);
            if (!entry.isCompleted) {
                const button = document.createElement("button");
                button.className = "secondary";
                button.textContent = "סימון כהוחזר";
                button.addEventListener("click", async () => {
                    button.disabled = true;
                    try {
                        await apiRequest(`/personal-transactions/${entry.id}/complete`, { method: "POST" });
                        await loadPersonalTransactions();
                    } catch (error) {
                        showMessage(openMessage, error.message, "error");
                        button.disabled = false;
                    }
                });
                row.append(button);
            }
            (entry.isCompleted ? historyList : openList).append(row);
        }
        applyPersonalItemSearch();
        if (!completed.length) {
            const empty = document.createElement("p");
            empty.textContent = "אין עדיין השאלות שהסתיימו.";
            historyList.append(empty);
        }
    } catch (error) {
        showMessage(openMessage, error.message, "error");
    }
}

document.getElementById("personalDate").value = new Date().toLocaleDateString("en-CA");
document.getElementById("personalForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const message = document.getElementById("personalMessage");
    const date = document.getElementById("personalDate").value;
    const reminder = document.getElementById("personalReminder").value;
    try {
        await apiRequest("/personal-transactions", {
            method: "POST",
            body: JSON.stringify({
                otherPartyName: document.getElementById("personalParty").value.trim(),
                itemDescription: document.getElementById("personalItem").value.trim(),
                type: Number(document.getElementById("personalType").value),
                amount: document.getElementById("personalAmount").value ? Number(document.getElementById("personalAmount").value) : null,
                transactionDate: new Date(date + "T12:00:00").toISOString(),
                reminderDate: reminder ? new Date(reminder + "T12:00:00").toISOString() : null
            })
        });
        event.target.reset();
        showMessage(message, "ההשאלה נשמרה בהצלחה.", "success");
        await loadPersonalTransactions();
    } catch (error) {
        showMessage(message, error.message, "error");
    }
});

function isAdminToken() {
    try {
        const encoded = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
        const data = JSON.parse(atob(encoded.padEnd(Math.ceil(encoded.length / 4) * 4, "=")));
        const role=data.role || data["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
        return role === "GmachAdmin" || (Array.isArray(role) && role.includes("GmachAdmin"));
    } catch { return false; }
}
async function initializeAdmin() {
    const section = document.getElementById("adminSection");
    section.hidden = false;
    updateNavigation();
    if (isAdminToken()) await refreshAdmin();
}
let ownerBorrowItemId=null;
const ownerBorrowDialog=document.getElementById("ownerBorrowDialog");
function openOwnerBorrow(item){
 ownerBorrowItemId=item.id;
 document.getElementById("ownerBorrowForm").reset();
 document.getElementById("ownerBorrowItemName").textContent=item.name;
 showMessage(document.getElementById("ownerBorrowMessage"),"");
 ownerBorrowDialog.showModal();
}
document.getElementById("cancelOwnerBorrow").addEventListener("click",()=>ownerBorrowDialog.close());
document.getElementById("ownerBorrowForm").addEventListener("submit",async event=>{
 event.preventDefault();
 const borrowerName=document.getElementById("ownerBorrowName").value.trim();
 const borrowerPhone=document.getElementById("ownerBorrowPhone").value.trim();
 const borrowerEmail=document.getElementById("ownerBorrowEmail").value.trim();
 const message=document.getElementById("ownerBorrowMessage");
 if(!borrowerName){showMessage(message,"חובה למלא שם שואל.","error");return;}
 if(!borrowerPhone&&!borrowerEmail){showMessage(message,"חובה למלא לפחות טלפון או אימייל.","error");return;}
 try{
  await apiRequest("/gmachs/mine/items/"+ownerBorrowItemId+"/borrow",{method:"POST",body:JSON.stringify({borrowerName,borrowerPhone:borrowerPhone||null,borrowerEmail:borrowerEmail||null})});
  ownerBorrowDialog.close();await refreshAdmin();await loadItems();
 }catch(error){showMessage(message,error.message,"error");}
});
document.getElementById("managedLoansSort").addEventListener("change", () => refreshAdmin());
const selectedAdminItems = new Map();
function updateBulkToolbar() {
    const count=selectedAdminItems.size;
    document.getElementById("bulkItemActions").hidden = count===0;
    document.getElementById("selectedItemsCount").textContent = count + " פריטים נבחרו";
}
async function editAdminItem(item) {
    const name=prompt("שם הפריט",item.name);
    if(name===null||!name.trim())return;
    const description=prompt("תיאור",item.description||"");
    if(description===null)return;
    try {
        await apiRequest("/items/"+item.id,{method:"PUT",body:JSON.stringify({name:name.trim(),description})});
        await refreshAdmin();await loadItems();
    } catch(e){showMessage(document.getElementById("adminMessage"),e.message,"error");}
}
function askCopyQuantity() {
    const input=prompt("כמה עותקים להוסיף לכל פריט? (1–1000)","1");
    if(input===null)return null;
    const quantity=Number(input);
    if(!Number.isInteger(quantity)||quantity<1||quantity>1000){
        showMessage(document.getElementById("adminMessage"),"יש להזין כמות שלמה בין 1 ל־1000.","error");
        return null;
    }
    return quantity;
}
async function addAdminCopies(items) {
    const quantity=askCopyQuantity();if(quantity===null)return;
    try {
        for(const item of items) await apiRequest("/items/"+item.id+"/copies/batch?quantity="+quantity,{method:"POST"});
        await refreshAdmin();await loadItems();
        showMessage(document.getElementById("adminMessage"),"העותקים נוספו בהצלחה.","success");
    }catch(e){showMessage(document.getElementById("adminMessage"),e.message,"error");}
}
document.getElementById("bulkEditItems").addEventListener("click",async()=>{
    const items=[...selectedAdminItems.values()];
    for(const item of items)await editAdminItem(item);
});
document.getElementById("bulkAddCopies").addEventListener("click",()=>addAdminCopies([...selectedAdminItems.values()]));
document.getElementById("bulkRemoveItems").addEventListener("click",async()=>{
    const items=[...selectedAdminItems.values()];
    if(!confirm("להסיר "+items.length+" פריטים מהרשימה הפעילה? ניתן לשחזר אותם בהמשך."))return;
    try {
        for(const item of items)await apiRequest("/items/"+item.id+"/active?isActive=false",{method:"PUT"});
        await refreshAdmin();await loadItems();
    }catch(e){showMessage(document.getElementById("adminMessage"),e.message,"error");}
});
async function refreshAdmin() {
    const items = document.getElementById("adminItems");
    const loans = document.getElementById("adminLoans");
    const message = document.getElementById("adminMessage");
    items.replaceChildren();
    loans.replaceChildren();
    try {
        let gmach;
        try { gmach = await apiRequest("/gmachs/mine"); }
        catch (error) {
            document.getElementById("claimGmachForm").hidden = false;
            document.getElementById("ownerManagement").hidden = true;
            showMessage(message,"יש לשייך גמ״ח לחשבון לפני ניהול פריטים.","");
            return;
        }
        document.getElementById("claimGmachForm").hidden = true;
        document.getElementById("ownerManagement").hidden = false;
        document.getElementById("ownerGmachInfo").textContent = "הגמ״ח שלי: " + gmach.name + " — " + gmach.city;
        const pendingRequests = loadBorrowRequests();
        const catalog = gmach.items;
        const removedList = document.getElementById("removedAdminItems");
        removedList.replaceChildren();
        selectedAdminItems.clear();
        updateBulkToolbar();
        for (const item of catalog) {
            const row = document.createElement("article");
            row.className = "item-card";
            row.dataset.itemSearch = [item.name, item.description].filter(Boolean).join(" ");
            const title = document.createElement("h3");
            title.textContent = item.name;
            const details = document.createElement("p");
            details.textContent = (item.description || "") + " | עותקים: " + item.totalCopies + " | זמינים: " + item.availableCopies;
            const active = document.createElement("button");
            active.textContent = item.isActive ? "הסרת פריט" : "שחזור פריט";
            active.onclick = async () => {
                try { await apiRequest("/items/"+item.id+"/active?isActive="+(!item.isActive),{method:"PUT"}); await refreshAdmin(); await loadItems(); }
                catch(e){showMessage(message,e.message,"error");}
            };
            row.append(title,details);
            if (item.isActive) {
                const selection = document.createElement("label");
                const checkbox = document.createElement("input");
                checkbox.type = "checkbox";
                checkbox.setAttribute("aria-label", "בחירת " + item.name);
                checkbox.addEventListener("change", () => {
                    if (checkbox.checked) selectedAdminItems.set(item.id,item);
                    else selectedAdminItems.delete(item.id);
                    updateBulkToolbar();
                });
                selection.append(checkbox,document.createTextNode(" בחירה"));
                const edit = document.createElement("button");
                edit.textContent = "עריכת פריט";
                edit.onclick = () => editAdminItem(item);
                const copy = document.createElement("button");
                copy.textContent = "הוספת עותקים";
                copy.onclick = () => addAdminCopies([item]);
                const borrow = document.createElement("button");
                borrow.textContent = "סימון כהושאל";
                borrow.disabled = item.availableCopies < 1;
                borrow.onclick = () => openOwnerBorrow(item);
                row.append(selection,edit,copy,borrow,active);
                items.append(row);
            } else {
                row.append(active);
                removedList.append(row);
            }
        }
        applyAdminItemSearch();
        if (!removedList.childElementCount) {
            const empty=document.createElement("p"); empty.textContent="אין פריטים לשחזור."; removedList.append(empty);
        }
        const managedContainer=document.getElementById("managedLoans");
        managedContainer.replaceChildren();
        const [managedLoans, activeLoans] = await Promise.all([apiRequest("/gmachs/mine/managed-loans"), apiRequest("/Loans/active")]);
        const sortDirection=document.getElementById("managedLoansSort").value;
        managedLoans.sort((a,b)=>sortDirection==="oldest"
          ? new Date(a.borrowedAt)-new Date(b.borrowedAt)
          : new Date(b.borrowedAt)-new Date(a.borrowedAt));
        for(const record of managedLoans){
          const row=document.createElement("article");row.className="item-card";
          const details=document.createElement("p");
          details.textContent=(record.itemName || "פריט") + " | קוד פריט: " + (record.itemId ?? "לא ידוע") + " | " + record.borrowerName + " | " + (record.borrowerPhone||"") + " | " + (record.borrowerEmail||"");
          const borrowedDate=document.createElement("p");
          borrowedDate.textContent="הושאל בתאריך: "+new Date(record.borrowedAt).toLocaleString("he-IL",{dateStyle:"short",timeStyle:"short"});
          const returned=document.createElement("button");returned.textContent="הוחזר";
          returned.onclick=async()=>{try{await apiRequest("/gmachs/mine/managed-loans/"+record.id+"/return",{method:"POST"});await refreshAdmin();await loadItems();}catch(e){showMessage(message,e.message,"error");}};
          row.append(details,borrowedDate,returned);managedContainer.append(row);
        }
        for (const loan of activeLoans) {
            const row = document.createElement("article");
            row.className = "item-card";
            const info = document.createElement("p");
            info.textContent = "השאלה "+loan.id+" | משתמש "+loan.userId+" | עותק "+loan.itemCopyId+" | עד "+new Date(loan.expectedReturnDate).toLocaleDateString("he-IL");
            const returned = document.createElement("button");
            returned.textContent = "הוחזר";
            returned.onclick = async () => {
                try {await apiRequest("/Loans/"+loan.id+"/admin-return",{method:"POST"});await refreshAdmin();await loadItems();}
                catch(e){showMessage(message,e.message,"error");}
            };
            const due = document.createElement("button");
            due.textContent = "שינוי תאריך";
            due.onclick = async () => {
                const value = prompt("תאריך חדש YYYY-MM-DD");
                if (!value) return;
                const date = new Date(value+"T12:00:00");
                if (isNaN(date.getTime())) return;
                try {await apiRequest("/Loans/"+loan.id+"/due-date",{method:"PUT",body:JSON.stringify({expectedReturnDate:date.toISOString()})});await refreshAdmin();}
                catch(e){showMessage(message,e.message,"error");}
            };
            row.append(info,returned,due);
            loans.append(row);
        }
        await pendingRequests;
    } catch(e) {showMessage(message,e.message,"error");}
}
document.getElementById("adminItemForm").addEventListener("submit",async event => {
    event.preventDefault();
    const message = document.getElementById("adminMessage");
    try {
        await apiRequest("/gmachs/mine/items",{method:"POST",body:JSON.stringify({
            name:document.getElementById("adminItemName").value.trim(),
            description:document.getElementById("adminItemDescription").value.trim()
        })});
        event.target.reset();
        await refreshAdmin(); await loadItems();
        showMessage(message,"הפריט נוסף","success");
    } catch(e){showMessage(message,e.message,"error");}
});


// Separate public catalog, login, personal account and owner management views.
function showPage(page, push = true) {
    if ((page === "account" || page === "owner" || page === "profile" || page === "siteAdmin") && !token) page = "login";
    if (page === "siteAdmin" && !isSiteAdminToken()) page = "home";
    if (page === "owner" && !isAdminToken()) page = "account";
    for(const info of ["about","privacy","contact"]){
        document.getElementById(info+"Page").hidden=page!==info;
    }
    document.getElementById("accountMenu").open=false;
    authSection.hidden = page !== "login";
    dashboardSection.hidden = page !== "home";
    document.getElementById("gmachPage").hidden = page !== "gmach";
    document.getElementById("accountPage").hidden = page !== "account";
    document.getElementById("profilePage").hidden = page !== "profile";
    document.getElementById("openLoansSection").hidden = page !== "account";
    document.getElementById("ownerPage").hidden = page !== "owner";
    document.getElementById("siteAdminPage").hidden = page !== "siteAdmin";
    document.getElementById("adminSection").hidden = page !== "owner";
    document.getElementById("accountMenu").hidden = !token;
    document.getElementById("logoutBtn").hidden = !token;
    document.getElementById("profileNav").hidden = !token;
    const activeNav={home:"homeNav",gmach:"homeNav",login:"loginNav",account:"myLoansNav",owner:"adminNav",siteAdmin:"siteAdminNav",profile:"profileNav"}[page];
    for(const id of ["homeNav","loginNav","myLoansNav","adminNav","siteAdminNav","profileNav"]){
        const button=document.getElementById(id);
        if(!button)continue;
        if(id===activeNav)button.setAttribute("aria-current","page");
        else button.removeAttribute("aria-current");
    }
    renderWelcomeName();
    if (token) void refreshWelcomeName();
    document.getElementById("loginNav").hidden = !!token;
    document.getElementById("myLoansNav").hidden = !token;
    document.getElementById("adminNav").hidden = !token || !isAdminToken();
    document.getElementById("siteAdminNav").hidden = !token || !isSiteAdminToken();
    if (push) {
        const url = new URL(location.href);
        if (page !== "gmach") url.searchParams.delete("gmach");
        if (page === "home" || page === "gmach") url.searchParams.delete("page");
        else url.searchParams.set("page", page);
        history.pushState({page}, "", url);
    }
    if (page === "home") { findGmachs(); }
    if (page === "gmach") { const id=Number(new URLSearchParams(location.search).get("gmach")); if(Number.isInteger(id)&&id>0)renderGmach(id);else showPage("home",true); }
    selectedChatGmach = null;
    if (page !== "gmach") openChatOnGmachLoad = null;
    document.getElementById("floatingGmachChat").classList.toggle("is-gmach", page === "gmach");
    const chat = document.getElementById("gmachChat");
    if (chat.open) chat.close();
    if (page === "account") { loadMyLoans(); loadPersonalTransactions(); loadBorrowApprovals(); }
    if (page === "owner") refreshAdmin();
    if (page === "profile") {
        loadProfile();
        mountGoogleButton("googleLink", document.getElementById("profileMessage"));
    }
    if (page === "login") mountGoogleButton("googleSignIn", authMessage);
    if (page === "siteAdmin") {
        loadSiteAdmin();
        loadSiteAdminOpenLoans();
    }
    window.scrollTo({top:0,behavior:window.matchMedia("(prefers-reduced-motion: reduce)").matches?"instant":"smooth"});
}
function showHome() { showPage("home"); }

async function loadProfile(){
 const msg=document.getElementById("profileMessage");showMessage(msg,"");
 try{
  const p=await apiRequest("/Auth/profile");
  document.getElementById("profileName").value=p.name||"";
  document.getElementById("profileEmail").value=p.email||"";
  document.getElementById("profileIsOwner").checked=!!p.isGmachOwner;
  document.getElementById("profileGmachName").value=p.gmachName||"";
  document.getElementById("profileGmachType").value=p.gmachType||"";
  document.getElementById("profileCity").value=normalizeCity(p.city);
  document.getElementById("profileGmachFields").hidden=!p.isGmachOwner;
  document.getElementById("profileCurrentPassword").value="";
  document.getElementById("profileNewPassword").value="";
 }catch(e){showMessage(msg,e.message,"error");}
}
document.getElementById("profileIsOwner").addEventListener("change",e=>{
 document.getElementById("profileGmachFields").hidden=!e.target.checked;
});
document.getElementById("profileShowPasswords").addEventListener("change",e=>{
 for(const id of ["profileCurrentPassword","profileNewPassword"])document.getElementById(id).type=e.target.checked?"text":"password";
});
document.getElementById("profileForm").addEventListener("submit",async e=>{
 e.preventDefault();const owner=document.getElementById("profileIsOwner").checked;
 const msg=document.getElementById("profileMessage");
 const payload={name:document.getElementById("profileName").value.trim(),email:document.getElementById("profileEmail").value.trim(),isGmachOwner:owner,
 gmachName:owner?document.getElementById("profileGmachName").value.trim():null,
 gmachType:owner?document.getElementById("profileGmachType").value.trim():null,
 city:owner?normalizeCity(document.getElementById("profileCity").value.trim()):null,
 currentPassword:document.getElementById("profileCurrentPassword").value,
 newPassword:document.getElementById("profileNewPassword").value||null};
 try{
  const result=await apiRequest("/Auth/profile",{method:"PUT",body:JSON.stringify(payload)});
  if(!result.accessToken)throw new Error("נדרש להתחבר מחדש.");
  token=result.accessToken;sessionStorage.setItem("gmachToken",token);
  signedInName = String(payload.name || "").trim().slice(0, 60);
  profileNameToken = token;
  renderWelcomeName();
  updateNavigation();await loadProfile();showMessage(msg,"הפרטים עודכנו בהצלחה.","success");
 }catch(err){showMessage(msg,err.message,"error");}
});
function updateNavigation() {
    document.getElementById("loginNav").hidden = !!token;
    document.getElementById("myLoansNav").hidden = !token;
    document.getElementById("adminNav").hidden = !token || !isAdminToken();
    document.getElementById("siteAdminNav").hidden = !token || !isSiteAdminToken();
    document.getElementById("accountMenu").hidden = !token;
}
document.getElementById("homeNav").addEventListener("click", showHome);
document.getElementById("loginNav").addEventListener("click", showLogin);
document.getElementById("myLoansNav").addEventListener("click", () => showPage("account"));
document.getElementById("adminNav").addEventListener("click", () => showPage("owner"));
document.getElementById("siteAdminNav").addEventListener("click", () => showPage("siteAdmin"));
document.getElementById("profileNav").addEventListener("click",()=>showPage("profile"));
document.querySelectorAll("[data-info-page]").forEach(button=>{
    button.addEventListener("click",()=>showPage(button.dataset.infoPage));
});
document.getElementById("footerYear").textContent=String(new Date().getFullYear());
document.addEventListener("click",event=>{
    const menu=document.getElementById("accountMenu");
    if(menu.open&&!menu.contains(event.target))menu.open=false;
});
document.addEventListener("keydown",event=>{
    if(event.key==="Escape")document.getElementById("accountMenu").open=false;
});
window.addEventListener("popstate", () => {
    const params=new URLSearchParams(location.search);
    const page=params.has("gmach")?"gmach":params.get("page")||"home";
    showPage(["login","account","owner","profile","gmach","siteAdmin","about","privacy","contact"].includes(page) ? page : "home", false);
});

async function loadMyLoans() {
    if (!token) return;
    const list = document.getElementById("myLoansList");
    const message = document.getElementById("myLoansMessage");
    list.replaceChildren();
    try {
        const loans = await apiRequest("/Loans/my");
        showMessage(message, loans.length ? "" : "עדיין אין השאלות מהגמ״ח.");
        for (const loan of loans) {
            const row = document.createElement("article"); row.className = "item-card";
            const title = document.createElement("h3");
            title.textContent = loan.itemName || "פריט מספר " + loan.itemCopyId;
            const details = document.createElement("p");
            const gmachDetails = [loan.gmachName, loan.gmachCity, loan.gmachDescription].filter(Boolean).join(" | ");
            details.textContent = gmachDetails ? "פרטי הגמ״ח: " + gmachDetails : "פרטי הגמ״ח אינם זמינים";
            const due = document.createElement("p");
            due.textContent = "החזרה צפויה: " + new Date(loan.expectedReturnDate).toLocaleDateString("he-IL");
            row.append(title,details,due); list.append(row);
        }
    } catch (error) { showMessage(message,error.message,"error"); }
}

// Gmach directory and owner onboarding
const ownerCheckbox=document.getElementById("registerOwner");
ownerCheckbox.addEventListener("change",()=>{document.getElementById("ownerFields").hidden=!ownerCheckbox.checked;});
function renderGmachDirectory(list, groups) {
 const cards = document.createDocumentFragment();
 for (const group of groups) {
  const card=document.createElement("article");card.className="item-card";
  const heading=document.createElement("h3");heading.textContent=group.name;
  const city=document.createElement("p");city.textContent=normalizeCity(group.city);
  const button=document.createElement("button");button.className="primary";button.textContent="דף הגמ״ח";button.addEventListener("click",()=>showGmach(group.id));
  card.append(heading,city,button);cards.append(card);
 }
 if (!groups.length) {
  const empty=document.createElement("p");empty.textContent="לא נמצאו גמ״חים.";cards.append(empty);
 }
 list.replaceChildren(cards);
}

async function findGmachs() {
 const sequence = ++gmachSearchSequence;
 const list = document.getElementById("gmachList");
 const term = document.getElementById("unifiedGmachSearch").value.trim();
 const snapshot = term ? null : readDirectorySnapshot();
 const status = createDataLoader(snapshot ? "מעדכן את רשימת הגמ״חים…" : "טוען גמ״חים…");
 if (snapshot) {
  renderGmachDirectory(list, snapshot);
  list.prepend(status);
 } else {
  list.replaceChildren(status);
 }
 try {
  const path = term ? "/gmachs?" + new URLSearchParams({query: term, city: normalizeCity(term)}) : "/gmachs";
  const groups = await apiRequest(path);
  if (sequence === gmachSearchSequence) renderGmachDirectory(list, groups);
 } catch (error) {
  if (sequence !== gmachSearchSequence) return;
  status.className = "message error";
  status.textContent = snapshot
   ? "לא ניתן לעדכן כעת. מוצגת הרשימה האחרונה שנשמרה."
   : "טעינת רשימת הגמ״חים נכשלה: " + error.message;
 }
}
function showGmach(id){
 const url=new URL(location.href);url.searchParams.delete("page");url.searchParams.set("gmach",id);history.pushState({page:"gmach"},"",url);
 showPage("gmach",false);
}
async function renderGmach(id){
 const sequence = ++gmachDetailsSequence;
 const container=document.getElementById("gmachDetails");
 container.replaceChildren(createDataLoader("טוען את פריטי הגמ״ח…"));
 try {const g=await apiRequest("/gmachs/"+id);if(sequence!==gmachDetailsSequence || document.getElementById("gmachPage").hidden)return;const section=document.createElement("section");section.className="gmach-content";
 const grid=document.createElement("div");grid.className="gmach-items-grid";
 const title=document.createElement("h2");title.textContent=g.name+" — "+normalizeCity(g.city);section.append(title);
 const desc=document.createElement("p");desc.textContent=g.description||"";section.append(desc);
 selectedChatGmach = g;
 if (openChatOnGmachLoad === id) {
  openChatOnGmachLoad = null;
  if (token) openCustomerChat(g);
 }
 for(const item of g.items.filter(item=>item.isActive&&item.availableCopies>0)){const card=document.createElement("article");card.className="item-card";
 const name=document.createElement("h3");name.textContent=item.name;
 const info=document.createElement("p");info.textContent=(item.description||"")+" | עותקים זמינים: "+item.availableCopies;
 const button=document.createElement("button");button.textContent="השאלה";button.disabled=!item.isActive||item.availableCopies<1;
 button.addEventListener("click",()=>openBorrowDialog(item));
 card.append(name,info,button);grid.append(card);}
 section.append(grid);
 container.replaceChildren(section);
 }catch(e){if(sequence!==gmachDetailsSequence)return;const message=document.createElement("p");message.className="message error";message.textContent=e.message;container.replaceChildren(message);}
}
document.getElementById("backToGmachs").addEventListener("click",showHome);
document.getElementById("findGmachs").addEventListener("click",findGmachs);
document.getElementById("unifiedGmachSearch").addEventListener("keydown",event=>{if(event.key==="Enter"){event.preventDefault();findGmachs();}});
const startupParams=new URLSearchParams(location.search);
const initialPage=startupParams.has("gmach")?"gmach":startupParams.get("page");
showPage(["login","account","owner","profile","gmach","siteAdmin","about","privacy","contact"].includes(initialPage)?initialPage:"home",false);
document.getElementById("excelImportForm").addEventListener("submit",async event=>{
 event.preventDefault();const file=document.getElementById("excelFile").files[0];if(!file)return;
 const form=new FormData();form.append("file",file);
 try{const response=await fetch(API_BASE+"/gmachs/mine/import",{method:"POST",headers:{Authorization:"Bearer "+token},body:form});
 const result=await response.json();if(!response.ok)throw new Error(result.message||"ייבוא נכשל");
 clearPublicData();
 showMessage(document.getElementById("adminMessage"),"יובאו "+result.imported+" פריטים בהצלחה","success");await refreshAdmin();await loadItems();await findGmachs();
 }catch(e){showMessage(document.getElementById("adminMessage"),e.message,"error");}
});

document.getElementById("claimGmachForm").addEventListener("submit",async event=>{
 event.preventDefault();
 try {
  await apiRequest("/gmachs/mine",{method:"POST",body:JSON.stringify({name:document.getElementById("claimGmachName").value.trim(),city:normalizeCity(document.getElementById("claimGmachCity").value.trim())})});
  await refreshAdmin();await findGmachs();
  showMessage(document.getElementById("adminMessage"),"הגמ״ח שויך לחשבון בהצלחה.","success");
 }catch(error){showMessage(document.getElementById("adminMessage"),error.message,"error");}
});

// Password visibility is local to each password field.
for (const [checkboxId, inputId] of [["showPassword","password"],["registrationShowPassword","registrationPassword"],["showNewPassword","newPassword"]]) {
    const checkbox = document.getElementById(checkboxId);
    const input = document.getElementById(inputId);
    if (checkbox && input) checkbox.addEventListener("change", event => {
        input.type = event.target.checked ? "text" : "password";
    });
}

async function loadBorrowRequests() {
 const list=document.getElementById("borrowRequestsList");
 const message=document.getElementById("borrowRequestsMessage");
 list.replaceChildren();
 try {
  const requests=await apiRequest("/borrow-requests/mine");
  showMessage(message,requests.length?"":"אין בקשות ממתינות.");
  for(const request of requests){
   const card=document.createElement("article");card.className="item-card";
   const heading=document.createElement("h3");heading.textContent=request.itemName;
   const contact=document.createElement("p");contact.textContent=[request.applicantName,request.phone,request.email].filter(Boolean).join(" | ");
   const button=document.createElement("button");button.className="primary";button.textContent="סימון כהושאל";
   button.addEventListener("click",async()=>{
    button.disabled=true;
    try{await apiRequest("/borrow-requests/mine/"+request.id+"/fulfill",{method:"POST"});await refreshAdmin();}
    catch(e){showMessage(message,e.message,"error");button.disabled=false;}
   });
   card.append(heading,contact,button);list.append(card);
  }
 }catch(e){showMessage(message,e.message,"error");}
}

function isSiteAdminToken() {
    if (!token) return false;
    try {
        const encoded=token.split(".")[1].replace(/-/g,"+").replace(/_/g,"/");
        const data=JSON.parse(atob(encoded.padEnd(Math.ceil(encoded.length/4)*4,"=")));
        const role=data.role||data["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
        return role==="SiteAdmin" || (Array.isArray(role) && role.includes("SiteAdmin"));
    } catch { return false; }
}
const siteAdminMessage=document.getElementById("siteAdminMessage");
const siteAdminSelections={users:new Set(),gmachs:new Set(),items:new Set()};
const siteAdminBulkConfig={
    users:{list:"siteAdminUsers",all:"siteAdminSelectAllUsers",count:"siteAdminUsersSelectedCount",delete:"siteAdminDeleteUsers",path:"users",noun:"משתמשים"},
    gmachs:{list:"siteAdminGmachs",all:"siteAdminSelectAllGmachs",count:"siteAdminGmachsSelectedCount",delete:"siteAdminDeleteGmachs",path:"gmachs",noun:"גמ״חים"},
    items:{list:"siteAdminItems",all:"siteAdminSelectAllItems",count:"siteAdminItemsSelectedCount",delete:"siteAdminDeleteItems",path:"items",noun:"פריטים"}
};
function siteAdminCheckbox(kind,id,description) {
    const label=document.createElement("label");
    label.className="site-admin-select-label";
    const checkbox=document.createElement("input");
    checkbox.type="checkbox";checkbox.className="site-admin-select";
    checkbox.dataset.kind=kind;checkbox.dataset.id=String(id);
    checkbox.setAttribute("aria-label","בחירת "+description);
    checkbox.checked=siteAdminSelections[kind].has(String(id));
    checkbox.addEventListener("change",()=>{
        if(checkbox.checked)siteAdminSelections[kind].add(String(id));
        else siteAdminSelections[kind].delete(String(id));
        updateSiteAdminBulk(kind);
    });
    label.append(checkbox,document.createTextNode(" בחירה"));
    return label;
}
function updateSiteAdminBulk(kind) {
    const config=siteAdminBulkConfig[kind],selected=siteAdminSelections[kind];
    const boxes=[...document.querySelectorAll("#"+config.list+" .site-admin-select")];
    const all=document.getElementById(config.all);
    all.checked=boxes.length>0&&boxes.every(box=>box.checked);
    all.indeterminate=boxes.some(box=>box.checked)&&!all.checked;
    document.getElementById(config.count).textContent=selected.size+" נבחרו";
    document.getElementById(config.delete).disabled=selected.size===0;
}
for(const [kind,config] of Object.entries(siteAdminBulkConfig)) {
    document.getElementById(config.all).addEventListener("change",event=>{
        document.querySelectorAll("#"+config.list+" .site-admin-select").forEach(box=>{
            if(box.closest("[hidden]"))return;
            box.checked=event.target.checked;
            if(box.checked)siteAdminSelections[kind].add(box.dataset.id);
            else siteAdminSelections[kind].delete(box.dataset.id);
        });
        updateSiteAdminBulk(kind);
    });
    document.getElementById(config.delete).addEventListener("click",async()=>{
        const ids=[...siteAdminSelections[kind]];
        if(!ids.length)return;
        if(!confirm("למחוק "+ids.length+" "+config.noun+"? פעולה זו בלתי הפיכה ועלולה להשפיע על נתונים קשורים."))return;
        const button=document.getElementById(config.delete);
        button.disabled=true;
        let deleted=0,failed=0;
        for(const id of ids) {
            try {
                await apiRequest("/site-admin/"+config.path+"/"+encodeURIComponent(id),{method:"DELETE"});
                siteAdminSelections[kind].delete(id);
                deleted++;
            } catch(error) {
                failed++;
                console.error("Bulk delete failed",kind,id,error);
            }
        }
        await loadSiteAdmin(true);
        showMessage(siteAdminMessage,"נמחקו "+deleted+" "+config.noun+(failed?"; "+failed+" מחיקות נכשלו. ניתן לנסות שוב.":"."),failed?"error":"success");
    });
}

function adminActionButton(label, handler, destructive=false) {
    const button=document.createElement("button");
    button.type="button";button.textContent=label;
    button.className=destructive?"secondary":"primary";
    button.addEventListener("click",handler);
    return button;
}
async function adminPerform(path,options,success) {
    try {
        await apiRequest(path,options);
        showMessage(siteAdminMessage,success,"success");
        await loadSiteAdmin(true);
    } catch(error) { showMessage(siteAdminMessage,error.message,"error"); }
}
async function loadSiteAdmin(keepMessage=false) {
    if(!isSiteAdminToken())return;
    if(!keepMessage)showMessage(siteAdminMessage,"טוען נתוני ניהול...", "loading");
    try {
        const [users,gmachs,items]=await Promise.all([
            apiRequest("/site-admin/users"),
            apiRequest("/site-admin/gmachs"),
            apiRequest("/site-admin/items")
        ]);
        const usersList=document.getElementById("siteAdminUsers");
        const gmachsList=document.getElementById("siteAdminGmachs");
        const itemsList=document.getElementById("siteAdminItems");
        usersList.replaceChildren();gmachsList.replaceChildren();itemsList.replaceChildren();
        for(const kind of Object.keys(siteAdminSelections))siteAdminSelections[kind].clear();
        for(const u of users) {
            const card=document.createElement("details");card.className="site-admin-user-row";
            const entry=document.createElement("div");entry.className="site-admin-user-entry";
            if(!u.isPrimary)entry.append(siteAdminCheckbox("users",u.id,u.name));
            entry.dataset.search=(u.name+" "+u.email+" "+u.role).toLocaleLowerCase();
            const heading=document.createElement("summary");
            const label=document.createElement("span");label.textContent=u.name;
            const meta=document.createElement("span");meta.className="site-admin-user-meta";meta.textContent=u.email+" | "+(u.role==="SiteAdmin"?"מנהל אתר":u.role==="GmachAdmin"?"בעל גמ״ח":"משתמש");
            heading.append(label,meta);card.append(heading);
            const actions=document.createElement("div");actions.className="site-admin-user-actions";
            // All management actions appear only when the user row is expanded.
            actions.append(adminActionButton("עריכת שם",async()=>{
                const name=prompt("שם חדש",u.name);
                if(name===null)return;
                if(!name.trim()){showMessage(siteAdminMessage,"יש להזין שם.","error");return;}
                await adminPerform("/site-admin/users/"+u.id,{method:"PUT",body:JSON.stringify({name:name.trim()})},"שם המשתמש עודכן.");
            }));
            if(!u.isPrimary) {
                actions.append(adminActionButton("עריכת אימייל",async()=>{
                    const next=prompt("כתובת אימייל חדשה",u.email);
                    if(next===null)return;
                    if(!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(next.trim())){showMessage(siteAdminMessage,"יש להזין כתובת אימייל תקינה.","error");return;}
                    await adminPerform("/site-admin/users/"+u.id+"/email",{method:"PUT",body:JSON.stringify({email:next.trim()})},"כתובת האימייל עודכנה. המשתמש יצטרך להתחבר מחדש.");
                }));
                if(u.role==="SiteAdmin")actions.append(adminActionButton("הסרת הרשאת מנהל",async()=>{
                    if(confirm("להסיר את הרשאת ניהול האתר מהחשבון "+u.email+"?"))
                        await adminPerform("/site-admin/administrators",{method:"POST",body:JSON.stringify({email:u.email,enabled:false})},"הרשאת המנהל הוסרה.");
                },true));
                else actions.append(adminActionButton("הפיכה למנהל אתר",async()=>{
                    if(confirm("לתת הרשאת ניהול מלאה לחשבון "+u.email+"?"))
                        await adminPerform("/site-admin/administrators",{method:"POST",body:JSON.stringify({email:u.email,enabled:true})},"הרשאת מנהל נוספה.");
                }));
                actions.append(adminActionButton("מחיקת משתמש",async()=>{
                    if(confirm("למחוק לצמיתות את החשבון "+u.email+"?"))
                        await adminPerform("/site-admin/users/"+u.id,{method:"DELETE"},"המשתמש נמחק.");
                },true));
            }
            card.append(actions);
            entry.append(card);usersList.append(entry);
        }
        for(const g of gmachs) {
            const card=document.createElement("article");card.className="item-card";
            card.append(siteAdminCheckbox("gmachs",g.id,g.name));
            const title=document.createElement("h3");title.textContent=g.name;
            const details=document.createElement("p");details.textContent=g.city+" | בעלים מספר "+g.ownerId;
            card.append(title,details);
            card.append(adminActionButton("צ׳אט עם מנהל הגמ״ח", () => openCustomerChat(g, true)));
            card.append(adminActionButton("עריכת פרטי גמ״ח",async()=>{
                const name=prompt("שם הגמ״ח",g.name);if(name===null)return;
                const city=prompt("עיר",g.city);if(city===null)return;
                const description=prompt("תיאור",g.description||"");if(description===null)return;
                if(!name.trim()||!city.trim()){showMessage(siteAdminMessage,"שם ועיר הם שדות חובה.","error");return;}
                await adminPerform("/site-admin/gmachs/"+g.id,{method:"PUT",body:JSON.stringify({name:name.trim(),city:city.trim(),description})},"הגמ״ח עודכן.");
            }));
            card.append(adminActionButton("מחיקת גמ״ח",async()=>{
                if(confirm("למחוק את הגמ״ח "+g.name+"? הפעולה בלתי הפיכה."))
                    await adminPerform("/site-admin/gmachs/"+g.id,{method:"DELETE"},"הגמ״ח נמחק.");
            },true));
            gmachsList.append(card);
        }
        for(const item of items) {
            const card=document.createElement("article");card.className="item-card";
            card.append(siteAdminCheckbox("items",item.id,item.name));
            const title=document.createElement("h3");title.textContent=item.name;
            const details=document.createElement("p");details.textContent="גמ״ח מספר "+(item.gmachId||"ללא שיוך");
            card.append(title,details);
            card.append(adminActionButton("עריכת פריט",async()=>{
                const name=prompt("שם הפריט",item.name);if(name===null)return;
                const description=prompt("תיאור",item.description||"");if(description===null)return;
                if(!name.trim()){showMessage(siteAdminMessage,"יש להזין שם פריט.","error");return;}
                await adminPerform("/site-admin/items/"+item.id,{method:"PUT",body:JSON.stringify({name:name.trim(),description})},"הפריט עודכן.");
            }));
            card.append(adminActionButton("מחיקת פריט",async()=>{
                if(confirm("למחוק את הפריט "+item.name+"? הפעולה בלתי הפיכה."))
                    await adminPerform("/site-admin/items/"+item.id,{method:"DELETE"},"הפריט נמחק.");
            },true));
            itemsList.append(card);
        }
        filterSiteAdminUsers();
        for(const kind of Object.keys(siteAdminSelections))updateSiteAdminBulk(kind);
        if(!keepMessage)showMessage(siteAdminMessage,"");
    }catch(error){showMessage(siteAdminMessage,error.message,"error");}
}
document.getElementById("siteAdminGrantForm").addEventListener("submit",async event=>{
    event.preventDefault();
    const email=document.getElementById("siteAdminEmail").value.trim();
    if(!confirm("להעניק הרשאת ניהול מלאה לכתובת "+email+"?"))return;
    await adminPerform("/site-admin/administrators",{method:"POST",body:JSON.stringify({email,enabled:true})},"המנהל נוסף בהצלחה. עליו להתחבר מחדש.");
    document.getElementById("siteAdminEmail").value="";
});

function filterSiteAdminUsers(){
    const query=document.getElementById("siteAdminUserSearch").value.trim().toLocaleLowerCase();
    const rows=document.querySelectorAll("#siteAdminUsers .site-admin-user-entry");
    let visible=0;
    rows.forEach(row=>{const match=row.dataset.search.includes(query);row.hidden=!match;if(match)visible++;});
    document.getElementById("siteAdminUserCount").textContent="מוצגים "+visible+" מתוך "+rows.length+" משתמשים";
}
document.getElementById("siteAdminUserSearch").addEventListener("input",filterSiteAdminUsers);

function updateOpenLoanControls() {
    const checkboxes = [...document.querySelectorAll("#siteAdminOpenLoans input")];
    const selected = checkboxes.filter(box => box.checked).length;
    const all = document.getElementById("siteAdminSelectOpenLoans");
    all.disabled = checkboxes.length === 0;
    all.checked = selected > 0 && selected === checkboxes.length;
    all.indeterminate = selected > 0 && selected < checkboxes.length;
    document.getElementById("siteAdminDeleteOpenLoans").disabled = selected === 0;
    document.getElementById("siteAdminDeleteAllOpenLoans").disabled = all.disabled;
}

async function loadSiteAdminOpenLoans(result = "") {
    const adminToken = token;
    const list = document.getElementById("siteAdminOpenLoans");
    const message = document.getElementById("siteAdminOpenLoansMessage");
    list.replaceChildren();
    updateOpenLoanControls();
    showMessage(message, "טוען השאלות פתוחות...", "loading");
    try {
        const loans = await apiRequest("/site-admin/open-loans");
        if (token !== adminToken) return;
        for (const loan of loans) {
            const row = document.createElement("label");
            row.className = "site-admin-select-label";
            const box = document.createElement("input");
            box.type = "checkbox";
            box.className = "site-admin-select";
            box.dataset.id = loan.id;
            box.dataset.kind = loan.kind;
            box.addEventListener("change", updateOpenLoanControls);
            const date = new Date(loan.borrowedAt).toLocaleDateString("he-IL");
            row.append(box, document.createTextNode(
                `${loan.itemName} · ${loan.borrowerName} · ${date} · השאלה מס׳ ${loan.id}`
            ));
            list.append(row);
        }
        updateOpenLoanControls();
        showMessage(message, [result, loans.length ? "" : "אין השאלות פתוחות."].filter(Boolean).join(" "), result ? "success" : undefined);
    } catch (error) {
        if (token === adminToken) showMessage(message, error.message, "error");
    }
}

async function deleteSiteAdminOpenLoans(all) {
    const boxes = [...document.querySelectorAll("#siteAdminOpenLoans input")];
    const loans = boxes.filter(box => all || box.checked).map(box => ({
        id: Number(box.dataset.id), kind: box.dataset.kind
    }));
    if (!loans.length || !window.confirm(
        `למחוק לצמיתות ${loans.length} השאלות פתוחות? הפעולה אינה ניתנת לביטול.`
    )) return;

    const list = document.getElementById("siteAdminOpenLoans");
    const message = document.getElementById("siteAdminOpenLoansMessage");
    list.replaceChildren();
    updateOpenLoanControls();
    showMessage(message, "מוחק השאלות...");
    try {
        const { deleted } = await apiRequest("/site-admin/open-loans/delete", {
            method: "POST",
            body: JSON.stringify({ confirmed: true, all, loans })
        });
        if (isSiteAdminToken()) await loadSiteAdminOpenLoans(`נמחקו ${deleted} השאלות פתוחות.`);
    } catch (error) {
        if (!isSiteAdminToken()) return;
        await loadSiteAdminOpenLoans();
        showMessage(message, error.message, "error");
    }
}

document.getElementById("siteAdminSelectOpenLoans").addEventListener("change", event => {
    document.querySelectorAll("#siteAdminOpenLoans input").forEach(box => {
        box.checked = event.target.checked;
    });
    updateOpenLoanControls();
});
document.getElementById("siteAdminDeleteOpenLoans").addEventListener("click", () => deleteSiteAdminOpenLoans(false));
document.getElementById("siteAdminDeleteAllOpenLoans").addEventListener("click", () => deleteSiteAdminOpenLoans(true));

async function loadBorrowApprovals() {
    if (approvalPending || !token || document.getElementById("accountPage").hidden || document.hidden) return;
    const session = token;
    approvalPending = true;
    const section = document.getElementById("borrowApprovals");
    const list = document.getElementById("borrowApprovalList");
    try {
        const approvals = await apiRequest("/gmach-communication/approvals");
        if (token !== session || document.getElementById("accountPage").hidden) return;

        list.replaceChildren(...approvals.map(approval => {
            const entry = document.createElement("li");
            entry.textContent = `הבקשה לפריט „${approval.itemName}” בגמ״ח „${approval.gmachName}” אושרה.`;
            return entry;
        }));
        section.hidden = approvals.length === 0;

        showMessage(document.getElementById("borrowApprovalStatus"), "");
        const next = approvals.find(approval => !approval.seen);
        if (!next || document.querySelector("dialog[open]:not(#gmachChat)")) return;
        openApprovalId = next.id;
        document.getElementById("approvalText").textContent =
            `בעל הגמ״ח „${next.gmachName}” אישר את בקשת ההשאלה שלך לפריט „${next.itemName}”.`;
        document.getElementById("borrowApprovalDialog").showModal();
    } catch (error) {
        if (token === session) showMessage(document.getElementById("borrowApprovalStatus"), error.message, "error");
    } finally {
        approvalPending = false;
    }
}

document.getElementById("borrowApprovalDialog").addEventListener("close", async () => {
    const id = openApprovalId;
    openApprovalId = null;
    if (!id || !token) return;
    try {
        await apiRequest(`/gmach-communication/approvals/${id}/seen`, {method: "POST"});
        loadBorrowApprovals();
    } catch (error) {
        showMessage(document.getElementById("borrowApprovalStatus"), error.message, "error");
    }
});
setInterval(loadBorrowApprovals, 15000);

function chatPath(chat) {
    const base = `/gmach-communication/${chat.gmachId}/messages`;
    return chat.customerId ? `${base}?customerId=${chat.customerId}` : base;
}

function showChatPanel(title) {
    const panel = document.getElementById("gmachChat");
    document.getElementById("gmachChatTitle").textContent = title;
    document.getElementById("gmachChatInbox").replaceChildren();
    document.getElementById("gmachChatProducts").replaceChildren();
    document.getElementById("gmachChatMessages").replaceChildren();
    document.getElementById("gmachChatInput").value = "";
    resizeChatInput();
    clearChatAttachment();
    document.getElementById("gmachChatSend").disabled = false;
    document.getElementById("gmachChatForm").hidden = true;
    showMessage(document.getElementById("gmachChatStatus"), "");
    if (!panel.open) panel.show();
}

function openCustomerChat(gmach, freeText = false) {
    showChatPanel(gmach.name);
    const chat = {gmachId: gmach.id, token};
    if (freeText) {
        startChat(chat);
        document.getElementById("gmachChatInput").focus();
        return;
    }
    const choices = document.getElementById("gmachChatProducts");
    const intro = document.createElement("p");
    intro.textContent = "באיזה מוצר מהגמ״ח אתם מתעניינים?";
    choices.append(intro);
    for (const item of gmach.items.filter(item => item.isActive)) {
        const button = document.createElement("button");
        button.type = "button";
        button.className = "secondary";
        button.textContent = item.name;
        button.addEventListener("click", () => sendChatMessage(chat, {itemId: item.id}, item.name));
        choices.append(button);
    }
    startChat(chat);
}

async function showOwnerChatInbox() {
    clearInterval(chatTimer);
    const inbox = {token};
    activeChat = inbox;
    showChatPanel("הודעות צ׳אט");
    const list = document.getElementById("gmachChatInbox");
    const status = document.getElementById("gmachChatStatus");
    showMessage(status, "טוען שיחות…", "loading");
    try {
        const threads = await apiRequest("/gmach-communication/inbox");
        if (activeChat !== inbox || token !== inbox.token) return;
        showMessage(status, threads.length ? "" : "אין שיחות עדיין.");
        for (const thread of threads) {
            const button = document.createElement("button");
            button.type = "button";
            button.className = "secondary";
            const sender = `${thread.customerName} · משתמש מס׳ ${thread.customerId}`;
            button.textContent = sender;
            button.addEventListener("click", () => {
                showChatPanel(`פנייה מאת ${sender}`);
                const back = document.createElement("button");
                back.type = "button";
                back.className = "secondary";
                back.textContent = "חזרה לשיחות";
                back.addEventListener("click", showOwnerChatInbox);
                document.getElementById("gmachChatInbox").append(back);
                startChat({...thread, token});
            });
            list.append(button);
        }
    } catch (error) {
        if (activeChat === inbox) showMessage(status, error.message, "error");
    }
}

function startChat(chat) {
    clearInterval(chatTimer);
    activeChat = chat;
    document.getElementById("gmachChatForm").hidden = false;
    resizeChatInput();
    document.getElementById("gmachChatMessages").replaceChildren(createDataLoader("טוען הודעות…"));
    refreshChat(chat);
    chatTimer = setInterval(() => refreshChat(chat), 8000);
}

async function refreshChat(chat) {
    if (chat.reading || chat.sending || activeChat !== chat || chat.token !== token || document.hidden) return;
    chat.reading = true;
    const messages = document.getElementById("gmachChatMessages");
    const status = document.getElementById("gmachChatStatus");
    try {
        const result = await apiRequest(chatPath(chat));
        if (chat.sending || activeChat !== chat || chat.token !== token || document.hidden) return;
        const signature = result.map(message => `${message.id}:${message.readByOwner}`).join("|");
        if (chat.signature !== signature || chat.pendingBubble) {
            const shouldScroll = chat.newest !== result.at(-1)?.id || chat.pendingBubble;
            chat.signature = signature;
            chat.newest = result.at(-1)?.id ?? null;
            chat.pendingBubble = null;
            messages.replaceChildren(...result.map(message => createChatBubble(
                message.text, message.mine,
                message.mine && !chat.customerId ? (message.readByOwner ? "read" : "sent") : null,
                message.attachmentName, message.id, chat.gmachId
            )));
            if (shouldScroll) messages.scrollTop = messages.scrollHeight;
        }
        const unread = chat.customerId && result.findLast(message => !message.mine && !message.readByOwner);
        if (unread && unread.id > (chat.readThrough ?? 0)) {
            await apiRequest(`/gmach-communication/${chat.gmachId}/messages/read?customerId=${chat.customerId}`, {
                method: "POST", body: JSON.stringify({throughId: unread.id})
            });
            chat.readThrough = unread.id;
        }
    } catch (error) {
        if (activeChat === chat) {
            if (chat.newest === undefined) messages.replaceChildren();
            showMessage(status, error.message, "error");
        }
    } finally {
        chat.reading = false;
    }
}

function createChatBubble(text, mine, receipt, attachmentName, messageId, gmachId) {
    const row = document.createElement("div");
    row.className = mine ? "chat-message-row chat-message-row-mine" : "chat-message-row";
    const bubble = document.createElement("div");
    bubble.className = mine ? "chat-message chat-message-mine" : "chat-message";
    const message = document.createElement("bdi");
    message.className = "chat-message-text";
    message.textContent = text;
    if (text) bubble.append(message);
    if (attachmentName) {
        const attachment = document.createElement(messageId ? "button" : "span");
        attachment.className = "chat-attachment";
        attachment.textContent = "📎 " + attachmentName;
        if (messageId) {
            attachment.type = "button";
            attachment.addEventListener("click", () => downloadChatAttachment(gmachId, messageId, attachmentName));
        }
        bubble.append(attachment);
    }
    row.append(bubble);
    if (receipt) {
        const marks = document.createElement("span");
        marks.className = `chat-ticks chat-ticks-${receipt}`;
        const label = {pending: "נשלחת", sent: "נשלחה", read: "נקראה על ידי בעל הגמ״ח"}[receipt];
        marks.setAttribute("role", "img");
        marks.setAttribute("aria-label", label);
        marks.title = label;
        for (let n = 0; n < 2; n++) {
            const tick = document.createElement("span");
            tick.textContent = "✓";
            tick.setAttribute("aria-hidden", "true");
            marks.append(tick);
        }
        row.prepend(marks);
    }
    return row;
}

async function sendChatMessage(chat, payload, productName = "", file = null) {
    if (!chat || chat.sending || activeChat !== chat || chat.token !== token) return;
    chat.sending = true;
    const submit = document.getElementById("gmachChatSend");
    const status = document.getElementById("gmachChatStatus");
    const messages = document.getElementById("gmachChatMessages");
    const text = payload.text || (productName ? `אני מתעניין בפריט ${productName}` : "");
    const pending = createChatBubble(text, true, chat.customerId ? null : "pending", file?.name);
    chat.pendingBubble = pending;
    messages.append(pending);
    messages.scrollTop = messages.scrollHeight;
    submit.disabled = true;
    showMessage(status, "שולח…");
    try {
        if (file) {
            const bytes = new Uint8Array(await file.arrayBuffer());
            const encoded = [];
            for (let start = 0; start < bytes.length; start += 8192) {
                encoded.push(String.fromCharCode(...bytes.subarray(start, start + 8192)));
            }
            if (activeChat !== chat || chat.token !== token) return;
            payload = {...payload, attachmentName: file.name, attachmentBase64: btoa(encoded.join(""))};
        }
        await apiRequest(chatPath(chat), {method: "POST", body: JSON.stringify(payload)});
        if (activeChat !== chat || chat.token !== token) return;
        const input = document.getElementById("gmachChatInput");
        if (payload.text && input.value.trim() === payload.text) {
            input.value = "";
            resizeChatInput();
        }
        if (file && document.getElementById("gmachChatFile").files[0] === file) clearChatAttachment();
        const sent = createChatBubble(text, true, chat.customerId ? null : "sent", file?.name);
        pending.replaceWith(sent);
        chat.pendingBubble = sent;
        showMessage(status, chat.customerId ? "ההודעה נשלחה." : "ממתין לנציג הגמ״ח", "success");
    } catch (error) {
        pending.remove();
        chat.pendingBubble = null;
        if (activeChat === chat) showMessage(status, error.message, "error");
    } finally {
        chat.sending = false;
        if (activeChat === chat) {
            submit.disabled = false;
            refreshChat(chat);
        }
    }
}

async function openFloatingGmachChat() {
    if (!token) {
        showChatPanel("צ׳אט עם בעל הגמ״ח");
        const notice = document.createElement("p");
        notice.className = "chat-greeting-bubble";
        notice.textContent = "בשביל להתכתב יש להתחבר.";
        const login = document.createElement("button");
        login.type = "button";
        login.className = "primary";
        login.textContent = "התחברות";
        login.addEventListener("click", showLogin);
        document.getElementById("gmachChatProducts").append(notice, login);
        return;
    }
    if (selectedChatGmach) {
        openCustomerChat(selectedChatGmach);
        return;
    }
    if (!document.getElementById("ownerManagement").hidden &&
        !document.getElementById("adminSection").hidden) {
        showOwnerChatInbox();
        return;
    }

    const chooser = {token};
    const fromSiteAdmin = isSiteAdminToken() && !document.getElementById("siteAdminPage").hidden;
    clearInterval(chatTimer);
    activeChat = chooser;
    showChatPanel(fromSiteAdmin ? "בחירת מנהל גמ״ח לשיחה" : "עם איזה גמ״ח תרצו להתכתב?");
    const status = document.getElementById("gmachChatStatus");
    showMessage(status, "טוען גמ״חים…", "loading");
    try {
        const gmachs = await apiRequest(fromSiteAdmin ? "/site-admin/gmachs" : "/gmachs");
        if (activeChat !== chooser || token !== chooser.token) return;
        showMessage(status, gmachs.length ? "" : "אין גמ״חים זמינים.");
        const inbox = document.getElementById("gmachChatInbox");
        for (const gmach of gmachs) {
            const choice = document.createElement("button");
            choice.type = "button";
            choice.className = "secondary";
            choice.textContent = `${gmach.name} · ${normalizeCity(gmach.city)}`;
            choice.addEventListener("click", () => {
                if (fromSiteAdmin) {
                    openCustomerChat(gmach, true);
                    return;
                }
                openChatOnGmachLoad = gmach.id;
                showGmach(gmach.id);
            });
            inbox.append(choice);
        }
    } catch (error) {
        if (activeChat === chooser) showMessage(status, error.message, "error");
    }
}

document.getElementById("floatingGmachChat").addEventListener("click", openFloatingGmachChat);

function resizeChatInput() {
    const input = document.getElementById("gmachChatInput");
    input.style.height = "auto";
    input.style.height = Math.min(input.scrollHeight, 120) + "px";
}

function clearChatAttachment() {
    document.getElementById("gmachChatFile").value = "";
    document.getElementById("gmachChatAttachmentPreview").hidden = true;
    document.getElementById("gmachChatAttachmentName").textContent = "";
}

async function downloadChatAttachment(gmachId, messageId, name) {
    const status = document.getElementById("gmachChatStatus");
    try {
        const response = await fetch(`${API_BASE}/gmach-communication/${gmachId}/messages/${messageId}/attachment`, {
            headers: {Authorization: `Bearer ${token}`}, cache: "no-store"
        });
        if (!response.ok) throw new Error("לא ניתן להוריד את הקובץ.");
        const url = URL.createObjectURL(await response.blob());
        const link = document.createElement("a");
        link.href = url;
        link.download = name;
        link.click();
        setTimeout(() => URL.revokeObjectURL(url), 60000);
    } catch (error) {
        showMessage(status, error.message, "error");
    }
}

const chatInput = document.getElementById("gmachChatInput");
const chatFile = document.getElementById("gmachChatFile");
const chatForm = document.getElementById("gmachChatForm");
chatInput.addEventListener("input", resizeChatInput);
chatInput.addEventListener("keydown", event => {
    if (event.key === "Enter" && !event.shiftKey && !event.isComposing && !event.repeat) {
        event.preventDefault();
        chatForm.requestSubmit();
    }
});
document.getElementById("gmachChatAttach").addEventListener("click", () => chatFile.click());
document.getElementById("gmachChatRemoveAttachment").addEventListener("click", clearChatAttachment);
chatFile.addEventListener("change", () => {
    const file = chatFile.files[0];
    if (!file) return clearChatAttachment();
    if (!/\.(jpe?g|png|pdf)$/i.test(file.name) || file.size === 0 || file.size > 1024 * 1024 || file.name.length > 150) {
        clearChatAttachment();
        showMessage(document.getElementById("gmachChatStatus"), "אפשר לצרף תמונת JPG או PNG או קובץ PDF עד 1 MB.", "error");
        return;
    }
    document.getElementById("gmachChatAttachmentName").textContent = file.name;
    document.getElementById("gmachChatAttachmentPreview").hidden = false;
    showMessage(document.getElementById("gmachChatStatus"), "");
});
chatForm.addEventListener("submit", event => {
    event.preventDefault();
    const text = chatInput.value.trim();
    const file = chatFile.files[0];
    if (text || file) sendChatMessage(activeChat, {text}, "", file);
});
document.getElementById("gmachChat").addEventListener("close", () => {
    if (document.getElementById("gmachChat").open) return;
    activeChat = null;
    clearInterval(chatTimer);
});
document.getElementById("ownerChatInbox").addEventListener("click", showOwnerChatInbox);

function loadGoogleIdentityScript() {
    if (window.google?.accounts?.id) return Promise.resolve();
    if (!googleScript) {
        googleScript = new Promise((resolve, reject) => {
            const script = document.createElement("script");
            script.src = "https://accounts.google.com/gsi/client";
            script.async = true;
            script.onload = resolve;
            script.onerror = () => {
                script.remove();
                googleScript = null;
                reject(new Error("לא ניתן לטעון כרגע את הכניסה דרך Google."));
            };
            document.head.append(script);
        });
    }
    return googleScript;
}

async function mountGoogleButton(sectionId, status) {
    const section = document.getElementById(sectionId);
    const button = section.querySelector(".google-identity-button");
    const sequence = ++googleMountSequence;
    const session = token;
    section.hidden = true;
    button.replaceChildren();
    try {
        const config = await apiRequest("/Auth/google/config", {method: "GET", cache: "no-store"});
        if (!config?.clientId || sequence !== googleMountSequence) return;
        await loadGoogleIdentityScript();
        if (sequence !== googleMountSequence || session !== token || section.parentElement.hidden) return;

        const googleId = window.google.accounts.id;
        googleId.initialize({
            client_id: config.clientId,
            nonce: config.nonce,
            callback: async ({credential}) => {
                if (sequence !== googleMountSequence || token !== session || section.parentElement.hidden) return;
                showMessage(status, "מאמת חשבון Google…");
                try {
                    const result = await apiRequest("/Auth/google", {
                        method: "POST", body: JSON.stringify({credential, nonce: config.nonce})
                    });
                    if (token !== session || section.parentElement.hidden) return;
                    if (!result?.accessToken) throw new Error("לא התקבל אישור התחברות.");
                    token = result.accessToken;
                    sessionStorage.setItem("gmachToken", token);
                    signedInName = "";
                    profileNameToken = null;
                    void refreshWelcomeName();
                    if (session) {
                        showMessage(status, "חשבון Google קושר בהצלחה.", "success");
                        mountGoogleButton(sectionId, status);
                    } else {
                        showMessage(status, "");
                        showDashboard();
                    }
                } catch (error) {
                    showMessage(status, error.message, "error");
                }
            }
        });
        section.hidden = false;
        googleId.renderButton(button, {
            type: "standard", theme: "outline", size: "large",
            text: session ? "continue_with" : "signin_with", locale: "he"
        });
    } catch (error) {
        if (sequence === googleMountSequence && !section.parentElement.hidden)
            showMessage(status, error.message, "error");
    }
}
