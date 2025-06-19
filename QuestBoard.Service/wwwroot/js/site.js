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

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    startAutoRefresh();
    
    // Add resize listener for masonry layout
    window.addEventListener('resize', handleResize);
    
    // Initialize masonry layout after a short delay
    setTimeout(layoutMasonry, 100);
});