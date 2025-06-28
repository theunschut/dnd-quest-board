// Add additional proposed date input
function addProposedDate() {
    const container = document.getElementById('proposed-dates');
    if (!container) return;
    
    const index = container.children.length;
    
    const div = document.createElement('div');
    div.className = 'mb-3 proposed-date-item';
    div.innerHTML = `
        <label class="form-label">Proposed Date ${index + 1}</label>
        <div class="input-group">
            <input type="datetime-local" name="Quest.ProposedDates[${index}]" class="form-control" required step="60">
            <button type="button" class="btn btn-outline-danger" onclick="removeProposedDate(this)">Remove</button>
        </div>
    `;
    
    container.appendChild(div);
    
    // Set default time to 18:00 for new input
    const newInput = div.querySelector('input[type="datetime-local"]');
    setDefaultDateTime(newInput);
}

// Remove proposed date input
function removeProposedDate(button) {
    const container = document.getElementById('proposed-dates');
    if (!container || container.children.length <= 1) return;
    
    button.closest('.proposed-date-item').remove();
    
    // Reindex remaining inputs
    const dateItems = container.querySelectorAll('.proposed-date-item');
    dateItems.forEach((item, index) => {
        const label = item.querySelector('label');
        const input = item.querySelector('input[type="datetime-local"]');
        if (label) label.textContent = `Proposed Date ${index + 1}`;
        if (input) input.name = `Quest.ProposedDates[${index}]`;
    });
}

// Auto-refresh quest pages every 30 seconds (excluding Details and Create pages)
function startAutoRefresh() {
    if (window.location.pathname.includes('/Quest/') && 
        !window.location.pathname.includes('/Quest/Details') && 
        !window.location.pathname.includes('/Quest/Create')) {
        setInterval(() => {
            window.location.reload();
        }, 30000);
    }
}

// Calculate optimal number of columns based on container width
function calculateColumns(containerWidth, cardWidth, gap) {
    return Math.floor((containerWidth + gap) / (cardWidth + gap));
}

// JavaScript masonry layout
function layoutMasonry() {
    const container = document.querySelector('.quest-board-container');
    if (!container) return;
    
    const cards = Array.from(container.children);
    if (cards.length === 0) return;
    
    const containerWidth = container.offsetWidth;
    const cardWidth = 280;
    const gap = 16; // 1rem
    const padding = 16; // 1rem container padding
    
    // Calculate available width inside padding
    const availableWidth = containerWidth - (padding * 2);
    const columnCount = calculateColumns(availableWidth, cardWidth, gap);
    
    // Reset container to block layout
    container.style.display = 'block';
    container.style.columnCount = 'auto';
    container.style.position = 'relative';
    
    // Initialize column heights
    const columnHeights = new Array(columnCount).fill(0);
    
    // Center the columns within the available space with additional margin
    const totalColumnsWidth = (columnCount * cardWidth) + ((columnCount - 1) * gap);
    const extraMargin = 40; // Additional margin on both sides
    const availableForCentering = availableWidth - (extraMargin * 2);
    const leftOffset = extraMargin + Math.max(0, (availableForCentering - totalColumnsWidth) / 2);
    
    // Position each card
    cards.forEach((card, index) => {
        // Find shortest column
        const shortestColumnIndex = columnHeights.indexOf(Math.min(...columnHeights));
        
        // Calculate left position with centering offset
        const leftPosition = leftOffset + (shortestColumnIndex * (cardWidth + gap));
        
        // Position the card
        card.style.position = 'absolute';
        card.style.left = `${leftPosition}px`;
        card.style.top = `${columnHeights[shortestColumnIndex]}px`;
        card.style.width = `${cardWidth}px`;
        
        // Update column height
        const cardHeight = card.offsetHeight || 392; // fallback height
        columnHeights[shortestColumnIndex] += cardHeight + gap;
    });
    
    // Set container height
    const maxHeight = Math.max(...columnHeights) - gap;
    container.style.height = `${maxHeight}px`;
}

// Debounced resize handler
let resizeTimeout;
function handleResize() {
    clearTimeout(resizeTimeout);
    resizeTimeout = setTimeout(() => {
        layoutMasonry();
    }, 250);
}

// Set default date and time to 18:00 for datetime-local inputs
function setDefaultDateTime(input) {
    if (!input || input.value) return; // Don't override existing values
    
    const now = new Date();
    const tomorrow = new Date(now);
    tomorrow.setDate(tomorrow.getDate() + 1);
    tomorrow.setHours(18, 0, 0, 0); // Set to 18:00:00
    
    // Format as YYYY-MM-DDTHH:MM for datetime-local input
    const year = tomorrow.getFullYear();
    const month = String(tomorrow.getMonth() + 1).padStart(2, '0');
    const day = String(tomorrow.getDate()).padStart(2, '0');
    const hours = String(tomorrow.getHours()).padStart(2, '0');
    const minutes = String(tomorrow.getMinutes()).padStart(2, '0');
    
    input.value = `${year}-${month}-${day}T${hours}:${minutes}`;
}

// Clean up datetime value to remove seconds and milliseconds
function cleanDateTimeValue(input) {
    if (!input || !input.value) return;
    
    // Parse the current value and remove seconds/milliseconds
    const date = new Date(input.value);
    if (isNaN(date.getTime())) return;
    
    // Set seconds and milliseconds to 0
    date.setSeconds(0, 0);
    
    // Format as YYYY-MM-DDTHH:MM for datetime-local input
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    
    input.value = `${year}-${month}-${day}T${hours}:${minutes}`;
}

// Make date blocks clickable for radio selection
function makeDataOptionsClickable() {
    // Handle Details page custom radio buttons
    const dateOptions = document.querySelectorAll('.date-option');
    dateOptions.forEach(dateOption => {
        dateOption.addEventListener('click', function(e) {
            // Prevent double-clicking on actual radio buttons/labels
            if (e.target.matches('input[type="radio"], .custom-radio-label, .custom-radio-label *')) {
                return;
            }
            
            // Find the first radio button in this date option
            const radioButtons = this.querySelectorAll('input[type="radio"]');
            if (radioButtons.length > 0) {
                // For custom radio groups, select the "Yes" option (value="2")
                const yesRadio = this.querySelector('input[type="radio"][value="2"]');
                if (yesRadio) {
                    yesRadio.checked = true;
                    yesRadio.dispatchEvent(new Event('change', { bubbles: true }));
                }
            }
        });
    });
    
    // Handle Manage page radio buttons
    const manageDateOptions = document.querySelectorAll('.manage-date-option');
    manageDateOptions.forEach(dateOption => {
        dateOption.addEventListener('click', function(e) {
            // Prevent double-clicking on actual radio buttons/labels
            if (e.target.matches('input[type="radio"], .form-check-label')) {
                return;
            }
            
            // Find the radio button in this date option
            const radioButton = this.querySelector('input[type="radio"]');
            if (radioButton) {
                radioButton.checked = true;
                radioButton.dispatchEvent(new Event('change', { bubbles: true }));
            }
        });
    });
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    startAutoRefresh();
    
    // Handle datetime-local inputs
    const datetimeInputs = document.querySelectorAll('input[type="datetime-local"]');
    datetimeInputs.forEach(input => {
        // For edit pages, clean existing values to remove seconds/milliseconds
        if (input.value) {
            cleanDateTimeValue(input);
        } else {
            // For create pages, set default time
            setDefaultDateTime(input);
        }
    });
    
    // Add resize listener for masonry layout
    window.addEventListener('resize', handleResize);
    
    // Initialize masonry layout after a short delay
    setTimeout(layoutMasonry, 100);
    
    // Make date options clickable
    makeDataOptionsClickable();
});