
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
    showHome();
    loadMyLoans();
    loadPersonalTransactions();
    if (isAdminToken()) refreshAdmin();
}

function showLogin() {
    showPage("login");
}

function logout() {
    sessionStorage.removeItem("gmachToken");
    token = null;
    currentItems = [];
    document.getElementById("welcomeText").textContent = "";
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

document.getElementById("forgotPasswordBtn").addEventListener("click",()=>{
 const notice=document.getElementById("forgotPasswordNotice");
 notice.hidden=false;
});

// התחברות
authForm.addEventListener("submit", async (event) => {
    event.preventDefault();

    if (authMode === "register") {
        const name = document.getElementById("fullName").value.trim();
        const email = document.getElementById("email").value.trim();
        const password = document.getElementById("password").value;

        if (password.length < 8) {
            showMessage(authMessage, "הסיסמה חייבת להכיל לפחות 8 תווים.", "error");
            document.getElementById("password").focus();
            return;
        }
        showMessage(authMessage, "מבצע הרשמה...");

        try {
            const registration = await apiRequest("/Auth/register", {
                method: "POST",
                body: JSON.stringify({ name, email, password, isGmachOwner: document.getElementById("registerOwner").checked, gmachName: document.getElementById("gmachName").value.trim(), city: document.getElementById("gmachCity").value.trim() })
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
    authForm.hidden=false;
    document.querySelector("#authSection .tabs").hidden=false;

    document.getElementById("loginTab")
        .classList.toggle("active", mode === "login");

    document.getElementById("registerTab")
        .classList.toggle("active", mode === "register");

    document.getElementById("nameGroup").hidden =
        mode !== "register";
    document.getElementById("ownerRegistration").hidden = mode !== "register";
    // Password recovery belongs to login only, never to registration.
    document.getElementById("forgotPasswordBtn").hidden = mode !== "login";
    document.getElementById("forgotPasswordNotice").hidden = true;
    document.getElementById("password").setAttribute("autocomplete", mode === "register" ? "new-password" : "current-password");
    document.getElementById("password").minLength = mode === "register" ? 8 : 0;
    document.getElementById("passwordHelp").hidden = mode !== "register";

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

document.getElementById("password").addEventListener("input",()=>{if(authMode==="register"&&document.getElementById("password").value.length>=8)showMessage(authMessage,"");});

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
        return (data.role || data["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]) === "GmachAdmin";
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
        const managedLoans=await apiRequest("/gmachs/mine/managed-loans");
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
    if ((page === "account" || page === "owner" || page === "profile") && !token) page = "login";
    if (page === "owner" && !isAdminToken()) page = "account";
    authSection.hidden = page !== "login";
    dashboardSection.hidden = page !== "home";
    document.getElementById("accountPage").hidden = page !== "account";
    document.getElementById("profilePage").hidden = page !== "profile";
    document.getElementById("openLoansSection").hidden = page !== "account";
    document.getElementById("ownerPage").hidden = page !== "owner";
    document.getElementById("adminSection").hidden = page !== "owner";
    document.getElementById("logoutBtn").hidden = !token;
    document.getElementById("profileNav").hidden = !token;
    document.getElementById("welcomeText").textContent = token ? "מחובר למערכת" : "";
    document.getElementById("loginNav").hidden = !!token;
    document.getElementById("myLoansNav").hidden = !token;
    document.getElementById("adminNav").hidden = !token || !isAdminToken();
    if (push) {
        const url = new URL(location.href);
        url.searchParams.delete("gmach");
        if (page === "home") url.searchParams.delete("page");
        else url.searchParams.set("page", page);
        history.pushState({page}, "", url);
    }
    if (page === "home") { findGmachs(); }
    if (page === "account") { loadMyLoans(); loadPersonalTransactions(); }
    if (page === "owner") refreshAdmin();
    if (page === "profile") loadProfile();
    window.scrollTo({top:0,behavior:"instant"});
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
  document.getElementById("profileCity").value=p.city||"";
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
 city:owner?document.getElementById("profileCity").value.trim():null,
 currentPassword:document.getElementById("profileCurrentPassword").value,
 newPassword:document.getElementById("profileNewPassword").value||null};
 try{
  const result=await apiRequest("/Auth/profile",{method:"PUT",body:JSON.stringify(payload)});
  if(!result.accessToken)throw new Error("נדרש להתחבר מחדש.");
  token=result.accessToken;sessionStorage.setItem("gmachToken",token);
  updateNavigation();await loadProfile();showMessage(msg,"הפרטים עודכנו בהצלחה.","success");
 }catch(err){showMessage(msg,err.message,"error");}
});
function updateNavigation() {
    document.getElementById("loginNav").hidden = !!token;
    document.getElementById("myLoansNav").hidden = !token;
    document.getElementById("adminNav").hidden = !token || !isAdminToken();
}
document.getElementById("homeNav").addEventListener("click", showHome);
document.getElementById("loginNav").addEventListener("click", showLogin);
document.getElementById("myLoansNav").addEventListener("click", () => showPage("account"));
document.getElementById("adminNav").addEventListener("click", () => showPage("owner"));
document.getElementById("profileNav").addEventListener("click",()=>showPage("profile"));
window.addEventListener("popstate", () => {
    const page = new URLSearchParams(location.search).get("page") || "home";
    showPage(["login","account","owner","profile"].includes(page) ? page : "home", false);
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
            const title = document.createElement("h3"); title.textContent = "השאלה מספר " + loan.id;
            const details = document.createElement("p"); details.textContent = "עותק " + loan.itemCopyId + " | החזרה צפויה: " + new Date(loan.expectedReturnDate).toLocaleDateString("he-IL");
            row.append(title,details); list.append(row);
        }
    } catch (error) { showMessage(message,error.message,"error"); }
}

async function loadCategories() {
    const select = document.getElementById("categoryFilter");
    try {
        const categories = await apiRequest("/categories");
        for (const category of categories) {
            const option = document.createElement("option");
            option.value = category.id;
            option.textContent = category.name;
            select.appendChild(option);
        }
    } catch (error) { showMessage(itemsMessage, error.message, "error"); }
}
loadCategories();

// Gmach directory and owner onboarding
const ownerCheckbox=document.getElementById("registerOwner");
ownerCheckbox.addEventListener("change",()=>{document.getElementById("ownerFields").hidden=!ownerCheckbox.checked;});
async function findGmachs(){
 const list=document.getElementById("gmachList");list.replaceChildren();document.getElementById("gmachDetails").replaceChildren();
 const query=new URLSearchParams({city:document.getElementById("citySearch").value.trim(),search:document.getElementById("gmachSearch").value.trim()});
 const itemTerm=document.getElementById("homeItemSearch").value.trim().toLocaleLowerCase("he");
 try {const groups=await apiRequest("/gmachs?"+query);
  let matches=0;
  for(const group of groups){
   if(itemTerm){const details=await apiRequest("/gmachs/"+group.id);if(!details.items.some(item=>item.isActive&&item.availableCopies>0&&item.name.toLocaleLowerCase("he").includes(itemTerm)))continue;}
   matches++;
   const card=document.createElement("article");card.className="item-card";
   const heading=document.createElement("h3");heading.textContent=group.name;
   const city=document.createElement("p");city.textContent=group.city;
   const button=document.createElement("button");button.className="primary";button.textContent="דף הגמ״ח";button.addEventListener("click",()=>showGmach(group.id));
   card.append(heading,city,button);list.append(card);
  }
  if(!matches){const empty=document.createElement("p");empty.textContent="לא נמצאו גמ״חים.";list.append(empty);}
 }catch(e){showMessage(itemsMessage,e.message,"error");}
}
async function showGmach(id){
 const url=new URL(location.href);url.searchParams.delete("page");url.searchParams.set("gmach",id);history.pushState(null,"",url);
 const container=document.getElementById("gmachDetails");container.replaceChildren();
 try {const g=await apiRequest("/gmachs/"+id);const section=document.createElement("section");section.className="auth-card wide-card";
 const title=document.createElement("h2");title.textContent=g.name+" — "+g.city;section.append(title);
 const desc=document.createElement("p");desc.textContent=g.description||"";section.append(desc);
 for(const item of g.items.filter(item=>item.isActive&&item.availableCopies>0&&item.name.toLocaleLowerCase("he").includes(document.getElementById("homeItemSearch").value.trim().toLocaleLowerCase("he")))){const card=document.createElement("article");card.className="item-card";
 const name=document.createElement("h3");name.textContent=item.name;
 const info=document.createElement("p");info.textContent=(item.description||"")+" | עותקים זמינים: "+item.availableCopies;
 const button=document.createElement("button");button.textContent="השאלה";button.disabled=!item.isActive||item.availableCopies<1;
 button.addEventListener("click",()=>{if(!token){showLogin();authSection.scrollIntoView();}else openBorrowDialog(item);});
 card.append(name,info,button);section.append(card);}
 container.append(section);container.scrollIntoView({behavior:"smooth"});
 }catch(e){showMessage(itemsMessage,e.message,"error");}
}
document.getElementById("findGmachs").addEventListener("click",findGmachs);
for(const id of ["citySearch","gmachSearch","homeItemSearch"])document.getElementById(id).addEventListener("keydown",event=>{if(event.key==="Enter"){event.preventDefault();findGmachs();}});
findGmachs();
const directGmachId=Number(new URLSearchParams(location.search).get("gmach"));
if(Number.isInteger(directGmachId)&&directGmachId>0)showGmach(directGmachId);
const initialPage=new URLSearchParams(location.search).get("page");
showPage(["login","account","owner","profile"].includes(initialPage)?initialPage:"home",false);
document.getElementById("excelImportForm").addEventListener("submit",async event=>{
 event.preventDefault();const file=document.getElementById("excelFile").files[0];if(!file)return;
 const form=new FormData();form.append("file",file);
 try{const response=await fetch(API_BASE+"/gmachs/mine/import",{method:"POST",headers:{Authorization:"Bearer "+token},body:form});
 const result=await response.json();if(!response.ok)throw new Error(result.message||"ייבוא נכשל");
 showMessage(document.getElementById("adminMessage"),"יובאו "+result.imported+" פריטים בהצלחה","success");await refreshAdmin();await loadItems();await findGmachs();
 }catch(e){showMessage(document.getElementById("adminMessage"),e.message,"error");}
});

document.getElementById("claimGmachForm").addEventListener("submit",async event=>{
 event.preventDefault();
 try {
  await apiRequest("/gmachs/mine",{method:"POST",body:JSON.stringify({name:document.getElementById("claimGmachName").value.trim(),city:document.getElementById("claimGmachCity").value.trim()})});
  await refreshAdmin();await findGmachs();
  showMessage(document.getElementById("adminMessage"),"הגמ״ח שויך לחשבון בהצלחה.","success");
 }catch(error){showMessage(document.getElementById("adminMessage"),error.message,"error");}
});

// Password visibility is local to each password field.
for (const [checkboxId, inputId] of [["showPassword","password"],["showNewPassword","newPassword"]]) {
    document.getElementById(checkboxId).addEventListener("change", event => {
        document.getElementById(inputId).type = event.target.checked ? "text" : "password";
    });
}
