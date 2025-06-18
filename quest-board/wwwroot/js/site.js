// Add additional proposed date input
function addProposedDate() {
    const container = document.getElementById('proposed-dates');
    const index = container.children.length;
    
    const div = document.createElement('div');
    div.className = 'mb-3';
    div.innerHTML = `
        <label class="form-label">Proposed Date ${index + 1}</label>
        <input type="datetime-local" name="ProposedDates[${index}]" class="form-control" required>
        <button type="button" class="btn btn-sm btn-outline-danger mt-1" onclick="removeProposedDate(this)">Remove</button>
    `;
    
    container.appendChild(div);
}

// Remove proposed date input
function removeProposedDate(button) {
    const container = document.getElementById('proposed-dates');
    if (container.children.length > 1) {
        button.closest('.mb-3').remove();
        
        // Reindex remaining inputs
        const inputs = container.querySelectorAll('input[type="datetime-local"]');
        inputs.forEach((input, index) => {
            input.name = `ProposedDates[${index}]`;
            const label = input.previousElementSibling;
            if (label && label.tagName === 'LABEL') {
                label.textContent = `Proposed Date ${index + 1}`;
            }
        });
    }
}

// Auto-refresh quest details every 30 seconds
function startAutoRefresh() {
    if (window.location.pathname.includes('/Quest/')) {
        setInterval(() => {
            window.location.reload();
        }, 30000);
    }
}