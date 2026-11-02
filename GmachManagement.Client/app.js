
const API_BASE = "https://localhost:7042/api";
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
    loadItems();
}

function showLogin() {
    authSection.hidden = false;
    dashboardSection.hidden = true;
    document.getElementById("logoutBtn").hidden = true;
    document.getElementById("welcomeText").textContent = "";
}

function logout() {
    sessionStorage.removeItem("gmachToken");
    token = null;
    currentItems = [];
    showLogin();
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

    if (response.status === 401) {
        logout();
        throw new Error(
            "יש להתחבר מחדש למערכת."
        );
    }

    if (response.status === 409) {
        throw new Error(
            "הפריט כבר אינו זמין. ייתכן שמשתמש אחר השאיל אותו."
        );
    }

    if (!response.ok) {
        let message = `שגיאת שרת (${response.status})`;

        const text = await response.text();

        try {
            const data = JSON.parse(text);
            message = data.message || data.title || message;
        } catch {
            if (text && text.length < 250) {
                message = text;
            }
        }

        throw new Error(message);
    }

    if (response.status === 204) {
        return null;
    }

    return response.json();
}

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
            showMessage(
                itemsMessage,
                "הפריט הושאל בהצלחה!",
                "success"
            );

        } catch (error) {
            showMessage(borrowMessage, error.message, "error");

            if (error.message.includes("אינו זמין")) {
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
    showLogin();
}
