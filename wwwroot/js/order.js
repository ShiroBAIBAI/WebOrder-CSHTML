// --- utils ---
const debounce = (fn, ms = 200) => {
    let t; return (...args) => { clearTimeout(t); t = setTimeout(() => fn(...args), ms); };
};
const slug = s => (s || "").toString().toLowerCase()
    .replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '');

// --- state ---
let lastQuery = "";
let lastItems = [];

// --- data ---
async function fetchMenu(q = "") {
    let data = [];
    try {
        const res = await fetch(`/api/menu?search=${encodeURIComponent(q)}`, { credentials: "same-origin" });
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        data = await res.json();
    } catch (err) {
        console.error("Menu API failed:", err);
        const box = document.getElementById("menuSections");
        if (box) box.innerHTML = `<div class="alert alert-danger">Failed to load menu (${err.message}).</div>`;
        return;
    }
    lastItems = data || [];
    renderMenu(lastItems);
}

// --- render ---
function renderMenu(items) {
    const byCat = new Map(); // key: catKey, value: {name, sort, items:[]}
    for (const x of items) {
        const catKey = x.categoryId || x.categoryName || "uncategorized";
        if (!byCat.has(catKey)) {
            byCat.set(catKey, {
                key: catKey,
                name: x.categoryName || "Uncategorized",
                sort: (typeof x.categorySort === "number") ? x.categorySort : 9999,
                items: []
            });
        }
        byCat.get(catKey).items.push(x);
    }

    // sort categories and items
    const cats = [...byCat.values()].sort((a, b) => a.sort - b.sort || a.name.localeCompare(b.name));
    cats.forEach(c => c.items.sort((a, b) => (a.name || "").localeCompare(b.name || "")));

    // category nav
    const navWrap = document.querySelector("#catNav .d-flex");
    navWrap.innerHTML = "";
    const allBtn = document.createElement("button");
    allBtn.type = "button";
    allBtn.className = "btn btn-sm btn-outline-secondary";
    allBtn.textContent = "All";
    allBtn.addEventListener("click", () => window.scrollTo({ top: document.querySelector("#catNav").offsetTop - 60, behavior: "smooth" }));
    navWrap.appendChild(allBtn);

    for (const c of cats) {
        const btn = document.createElement("button");
        btn.type = "button";
        btn.className = "btn btn-sm btn-outline-primary";
        btn.textContent = `${c.name} (${c.items.length})`;
        const id = `cat-${slug(c.name)}`;
        btn.addEventListener("click", () => {
            const el = document.getElementById(id);
            if (!el) return;
            const y = el.getBoundingClientRect().top + window.scrollY - 80; // offset for sticky nav
            window.scrollTo({ top: y, behavior: "smooth" });
        });
        navWrap.appendChild(btn);
    }

    // sections
    const box = document.getElementById("menuSections");
    box.innerHTML = "";
    if (cats.length === 0) {
        box.innerHTML = `<div class="alert alert-warning">No items match your search.</div>`;
        return;
    }

    for (const c of cats) {
        const sec = document.createElement("section");
        const id = `cat-${slug(c.name)}`;
        sec.innerHTML = `
      <h3 id="${id}" class="mt-4 mb-3">${c.name}</h3>
      <hr class="my-3"/>
      <div class="row g-3"></div>
    `;
        const row = sec.querySelector(".row");
        for (const x of c.items) {
            const col = document.createElement("div");
            col.className = "col-12 col-md-6 col-lg-4";
            col.innerHTML = `
        <div class="card h-100">
          <img class="card-img-top" src="${(x.images?.[0]) ?? '/img/placeholder.png'}" alt="image">
          <div class="card-body d-flex flex-column">
            <h5 class="card-title">${x.name}</h5>
            <p class="card-text text-muted small flex-grow-1">${x.desc ?? ''}</p>
            <div class="d-flex justify-content-between align-items-center">
              <span class="fw-bold">RM ${Number(x.price || 0).toFixed(2)}</span>
              <form class="add-form d-flex align-items-center gap-2">
                <input type="hidden" name="menuItemId" value="${x.id}">
                <input type="number" name="qty" value="1" min="1" class="form-control form-control-sm" style="width:90px"/>
                <button class="btn btn-primary btn-sm">Add</button>
              </form>
            </div>
          </div>
        </div>`;
            row.appendChild(col);

            // add-to-cart
            col.querySelector(".add-form").addEventListener("submit", async (e) => {
                e.preventDefault();
                const fd = new FormData(e.currentTarget);
                const r = await fetch("/Order/AddToCart", {
                    method: "POST",
                    body: fd,
                    headers: { "Accept": "application/json", "X-Requested-With": "XMLHttpRequest" },
                    credentials: "same-origin"
                });
                try {
                    const out = await r.json();
                    if (out?.ok) (window.showOK ? showOK(out.message || "Added to cart") : alert("Added to cart"));
                } catch {
                    (window.showOK ? showOK("Added to cart") : alert("Added to cart"));
                }
            });
        }
        box.appendChild(sec);
    }

    // highlight current category while滚动（可选增强）
    const headings = [...document.querySelectorAll("#menuSections h3[id]")];
    if ("IntersectionObserver" in window && headings.length) {
        const chips = [...navWrap.querySelectorAll(".btn-outline-primary")];
        const map = new Map(headings.map(h => [h.id, h]));
        const io = new IntersectionObserver(entries => {
            const visible = entries.filter(e => e.isIntersecting).sort((a, b) => b.intersectionRatio - a.intersectionRatio)[0];
            if (!visible) return;
            const id = visible.target.id;
            chips.forEach(btn => btn.classList.remove("active"));
            const idx = headings.findIndex(h => h.id === id);
            if (idx >= 0 && chips[idx]) chips[idx].classList.add("active");
        }, { rootMargin: "-60px 0px -70% 0px", threshold: [0, .25, .5, .75, 1] });
        headings.forEach(h => io.observe(h));
    }
}

// --- wire ---
document.addEventListener("DOMContentLoaded", () => {
    const q = document.getElementById("q");
    if (q) q.addEventListener("input", debounce(() => {
        lastQuery = q.value.trim();
        fetchMenu(lastQuery);
    }, 200));
    fetchMenu("");
});
