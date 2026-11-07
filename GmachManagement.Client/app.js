
const API_BASE = "https://gmachmanagement-api.onrender.com/api";
const PAGE_SIZE = 6;

const authSection = document.getElementById("authSection");
const dashboardSection = document.getElementById("dashboardSection");
const authForm = document.getElementById("authForm");
const authMessage = document.getElementById("authMessage");
const itemsMessage = document.getElementById("itemsMessage");
const borrowMessage = document.getElementById("borrowMessage");
const borrowDialog = document.getElementById("borrowDialog");

let token = sessionStorage.getItem("gmachToken");
let currentPage = 1;
let currentItems = [];
let selectedCopyId = null;
let authMode = "login";

function showMessage(element, message, type = "") {
    element.textContent = message;
    element.className = `message ${type}`;
}

function showDashboard() {
    authSection.hidden = true;
    dashboardSection.hidden = false;
    document.getElementById("logoutBtn").hidden = false;
    document.getElementById("welcomeText").textContent =
        "מחובר למערכת";
    updateNavigation();
    loadItems();
    loadPersonalTransactions();
    loadMyLoans();
    initializeAdmin();
}

function showLogin() {
    authSection.hidden = false;
    dashboardSection.hidden = true;
    document.getElementById("logoutBtn").hidden = true;
    document.getElementById("welcomeText").textContent = "";
    updateNavigation();
}

function logout() {
    sessionStorage.removeItem("gmachToken");
    token = null;
    currentItems = [];
    showLogin();
    showHome();
}

async function apiRequest(path, options = {}) {
    const headers = new Headers(options.headers || {});

    if (token) {
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
    } catch {
        throw new Error(
            "לא ניתן להתחבר לשרת. ודאי שה־API פועל."
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

    return response.json();
}

const forgotForm = document.getElementById("forgotPasswordForm");
const resetForm = document.getElementById("resetPasswordForm");
const resetToken = new URLSearchParams(location.search).get("resetToken");
if (resetToken) {
    resetForm.hidden = false;
    authForm.hidden = true;
    document.querySelector(".tabs").hidden = true;
    document.getElementById("forgotPasswordBtn").hidden = true;
}
document.getElementById("forgotPasswordBtn").addEventListener("click", () => {
    forgotForm.hidden = !forgotForm.hidden;
    document.getElementById("resetEmail").value = document.getElementById("email").value;
});
forgotForm.addEventListener("submit", async event => {
    event.preventDefault();
    try {
        await apiRequest("/Auth/forgot-password", {
            method: "POST",
            body: JSON.stringify({ email: document.getElementById("resetEmail").value.trim() })
        });
        showMessage(authMessage, "אם הכתובת רשומה, יישלח אליה קישור לאיפוס.", "success");
    } catch (error) { showMessage(authMessage, error.message, "error"); }
});
resetForm.addEventListener("submit", async event => {
    event.preventDefault();
    try {
        await apiRequest("/Auth/reset-password", {
            method: "POST",
            body: JSON.stringify({ token: resetToken, newPassword: document.getElementById("newPassword").value })
        });
        history.replaceState(null, "", location.pathname);
        resetForm.hidden = true;
        authForm.hidden = false;
        document.querySelector(".tabs").hidden = false;
        document.getElementById("forgotPasswordBtn").hidden = false;
        showMessage(authMessage, "הסיסמה עודכנה. ניתן להתחבר.", "success");
    } catch (error) { showMessage(authMessage, error.message, "error"); }
});

// התחברות
authForm.addEventListener("submit", async (event) => {
    event.preventDefault();

    if (authMode === "register") {
        const name = document.getElementById("fullName").value.trim();
        const email = document.getElementById("email").value.trim();
        const password = document.getElementById("password").value;

        showMessage(authMessage, "מבצע הרשמה...");

        try {
            await apiRequest("/Auth/register", {
                method: "POST",
                body: JSON.stringify({ name, email, password })
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

        return;
    }


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
        showMessage(authMessage, "");
        showDashboard();
    } catch (error) {
        showMessage(authMessage, error.message, "error");
    }
});

function switchAuthMode(mode) {
    authMode = mode;

    document.getElementById("loginTab")
        .classList.toggle("active", mode === "login");

    document.getElementById("registerTab")
        .classList.toggle("active", mode === "register");

    document.getElementById("nameGroup").hidden =
        mode !== "register";

    document.getElementById("authSubmit").textContent =
        mode === "login" ? "התחברות" : "הרשמה";

    showMessage(authMessage, "");
}

document.getElementById("loginTab").addEventListener(
    "click", () => switchAuthMode("login")
);

document.getElementById("registerTab").addEventListener(
    "click", () => switchAuthMode("register")
);

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

    grid.replaceChildren();
    showMessage(itemsMessage, "טוען פריטים...");

    try {
        const items = await apiRequest(
            `/items?${params.toString()}`
        );

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
        showMessage(itemsMessage, error.message, "error");
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
        if (!token) borrowButton.textContent = "התחברות להשאלה";

        borrowButton.addEventListener("click", () => {
            if (!token) { showLogin(); authSection.scrollIntoView({behavior:"smooth"}); return; }
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
        const copies = await apiRequest(
            `/items/${item.id}/copies`
        );

        // ItemStatus.Available = 1
        const availableCopy = copies.find(
            copy => copy.status === 1
        );

        if (!availableCopy) {
            showMessage(
                itemsMessage,
                "אין כרגע עותק זמין להשאלה.",
                "error"
            );
            await loadItems();
            return;
        }

        selectedCopyId = availableCopy.id;

        document.getElementById("borrowItemName").textContent =
            item.name;

        const returnDate = document.getElementById("returnDate");
        const tomorrow = new Date();
        tomorrow.setDate(tomorrow.getDate() + 1);

        const year = tomorrow.getFullYear();
        const month = String(tomorrow.getMonth() + 1).padStart(2, "0");
        const day = String(tomorrow.getDate()).padStart(2, "0");

        returnDate.min = `${year}-${month}-${day}`;
        returnDate.value = returnDate.min;

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

        const dateValue =
            document.getElementById("returnDate").value;

        const expectedReturnDate =
            new Date(`${dateValue}T12:00:00`).toISOString();

        const submitButton =
            document.querySelector(
                '#borrowForm button[type="submit"]'
            );

        submitButton.disabled = true;
        showMessage(borrowMessage, "מבצע השאלה...");

        try {
            await apiRequest("/Loans", {
                method: "POST",
                body: JSON.stringify({
                    itemCopyId: selectedCopyId,
                    expectedReturnDate
                })
            });

            borrowDialog.close();
            selectedCopyId = null;

            await loadItems();
            await loadMyLoans();
            showMessage(
                itemsMessage,
                "הפריט הושאל בהצלחה!",
                "success"
            );

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
async function loadPersonalTransactions() {
    const list = document.getElementById("personalList");
    const message = document.getElementById("personalMessage");
    list.replaceChildren();
    try {
        const entries = await apiRequest("/personal-transactions");
        if (!entries.length) {
            showMessage(message, "עדיין לא נוספו השאלות אישיות.");
            return;
        }
        showMessage(message, "");
        for (const entry of entries) {
            const row = document.createElement("article");
            row.className = "item-card";
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
                        showMessage(message, error.message, "error");
                        button.disabled = false;
                    }
                });
                row.append(button);
            }
            list.append(row);
        }
    } catch (error) {
        showMessage(message, error.message, "error");
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
        return (data.role || data["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]) === "GmachAdmin";
    } catch { return false; }
}
async function initializeAdmin() {
    const section = document.getElementById("adminSection");
    section.hidden = !isAdminToken();
    updateNavigation();
    if (!section.hidden) await refreshAdmin();
}
async function refreshAdmin() {
    const items = document.getElementById("adminItems");
    const loans = document.getElementById("adminLoans");
    const message = document.getElementById("adminMessage");
    items.replaceChildren();
    loans.replaceChildren();
    try {
        const catalog = await apiRequest("/items?page=1&pageSize=100");
        for (const item of catalog) {
            const row = document.createElement("article");
            row.className = "item-card";
            const title = document.createElement("h3");
            title.textContent = item.name;
            const edit = document.createElement("button");
            edit.textContent = "עריכת פריט";
            edit.onclick = async () => {
                const name = prompt("שם", item.name);
                if (!name || !name.trim()) return;
                const description = prompt("תיאור", item.description || "");
                if (description === null) return;
                try {
                    await apiRequest("/items/" + item.id, {method:"PUT",body:JSON.stringify({name,description})});
                    await refreshAdmin(); await loadItems();
                } catch(e) {showMessage(message,e.message,"error");}
            };
            const copy = document.createElement("button");
            copy.textContent = "הוספת עותק";
            copy.onclick = async () => {
                try {await apiRequest("/items/"+item.id+"/copies",{method:"POST"});await refreshAdmin();await loadItems();}
                catch(e){showMessage(message,e.message,"error");}
            };
            const active = document.createElement("button");
            active.textContent = item.isActive ? "הסרת פריט" : "שחזור פריט";
            active.onclick = async () => {
                try {await apiRequest("/items/"+item.id+"/active?isActive="+(!item.isActive),{method:"PUT"});await refreshAdmin();await loadItems();}
                catch(e){showMessage(message,e.message,"error");}
            };
            row.append(title,edit,copy,active);
            items.append(row);
        }
        const activeLoans = await apiRequest("/Loans/active");
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
    } catch(e) {showMessage(message,e.message,"error");}
}
document.getElementById("adminItemForm").addEventListener("submit",async event => {
    event.preventDefault();
    const message = document.getElementById("adminMessage");
    try {
        await apiRequest("/items",{method:"POST",body:JSON.stringify({
            name:document.getElementById("adminItemName").value.trim(),
            description:document.getElementById("adminItemDescription").value.trim()
        })});
        event.target.reset();
        await refreshAdmin(); await loadItems();
        showMessage(message,"הפריט נוסף","success");
    } catch(e){showMessage(message,e.message,"error");}
});


// Public catalog and the two role-based user journeys.
function showHome() {
    authSection.hidden = true;
    dashboardSection.hidden = false;
    document.getElementById("logoutBtn").hidden = !token;
    updateNavigation();
    loadItems();
    document.getElementById("personalSection").hidden = !token;
    document.getElementById("myLoansSection").hidden = !token;
    document.getElementById("adminSection").hidden = !token || !isAdminToken();
}
function updateNavigation() {
    document.getElementById("loginNav").hidden = !!token;
    document.getElementById("myLoansNav").hidden = !token;
    document.getElementById("adminNav").hidden = !token || !isAdminToken();
    document.getElementById("personalSection").hidden = !token;
    document.getElementById("myLoansSection").hidden = !token;
}
document.getElementById("homeNav").addEventListener("click", () => { showHome(); window.scrollTo({top:0,behavior:"smooth"}); });
document.getElementById("loginNav").addEventListener("click", () => { showLogin(); authSection.scrollIntoView({behavior:"smooth"}); });
document.getElementById("myLoansNav").addEventListener("click", () => { showHome(); document.getElementById("myLoansSection").scrollIntoView({behavior:"smooth"}); });
document.getElementById("adminNav").addEventListener("click", () => { showHome(); document.getElementById("adminSection").scrollIntoView({behavior:"smooth"}); });
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
            const title = document.createElement("h3"); title.textContent = "השאלה מספר " + loan.id;
            const details = document.createElement("p"); details.textContent = "עותק " + loan.itemCopyId + " | החזרה צפויה: " + new Date(loan.expectedReturnDate).toLocaleDateString("he-IL");
            row.append(title,details); list.append(row);
        }
    } catch (error) { showMessage(message,error.message,"error"); }
}
