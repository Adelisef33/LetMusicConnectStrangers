// Reviews Page JavaScript
(function() {
    'use strict';

    // State management
    const state = {
        selectedTrackId: null,
        selectedRating: 0
    };

    // Initialize on DOM load
    document.addEventListener('DOMContentLoaded', initializePage);

    function initializePage() {
        console.log('DOM loaded, initializing reviews page...');
        
        initializeTabs();
        initializeTrackSelection();
        initializeStarRating();
        initializeFormHandlers();
        initializeDeleteModal();
        initializeSearch();
        
        // Load initial state from form
        loadInitialState();
    }

    function initializeTabs() {
        const tabButtons = document.querySelectorAll('.tab-btn');
        
        tabButtons.forEach(btn => {
            btn.addEventListener('click', function() {
                const tabName = this.getAttribute('data-tab');
                switchTab(tabName);
            });
        });
    }

    function switchTab(tabName) {
        // Update buttons
        document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
        document.querySelector(`[data-tab="${tabName}"]`)?.classList.add('active');
        
        // Update panels
        document.querySelectorAll('.tab-panel').forEach(p => p.classList.remove('active'));
        document.getElementById(`panel-${tabName}`)?.classList.add('active');
    }

    // Track Selection
    function initializeTrackSelection() {
        // Use event delegation for dynamic track lists
        document.querySelectorAll('.tracks-list').forEach(container => {
            container.addEventListener('click', handleTrackClick);
        });
    }

    function handleTrackClick(e) {
        const trackItem = e.target.closest('.track-item');
        if (!trackItem) return;

        selectTrack({
            id: trackItem.getAttribute('data-track-id'),
            name: trackItem.getAttribute('data-track-name'),
            artist: trackItem.getAttribute('data-track-artist'),
            album: trackItem.getAttribute('data-track-album'),
            image: trackItem.getAttribute('data-track-image')
        });
    }

    function selectTrack(track) {
        console.log('Track selected:', track.name);
        
        // Update visual selection
        document.querySelectorAll('.track-item').forEach(i => i.classList.remove('selected'));
        const selectedItem = document.querySelector(`[data-track-id="${track.id}"]`);
        if (selectedItem) {
            selectedItem.classList.add('selected');
        }
        
        // Preserve ReviewId if editing
        const reviewId = getInputValue('inputReviewId');
        
        // IMMEDIATELY update form fields (don't wait for submit)
        setInputValue('inputTrackId', track.id);
        setInputValue('inputTrackName', track.name);
        setInputValue('inputArtistName', track.artist);
        setInputValue('inputAlbumName', track.album);
        setInputValue('inputAlbumImage', track.image);
        
        // Restore ReviewId if we had one
        if (reviewId) {
            setInputValue('inputReviewId', reviewId);
        }
        
        // Log what we set
        console.log('Hidden fields set:', {
            trackId: track.id,
            trackName: track.name,
            artist: track.artist
        });
        
        // Update preview
        updateSongPreview(track);
        updateSubmitButton();
    }

    function updateSongPreview(track) {
        const preview = document.getElementById('songPreview');
        const noSong = document.getElementById('noSongMsg');
        const previewImg = document.getElementById('previewImg');
        const previewName = document.getElementById('previewName');
        const previewArtist = document.getElementById('previewArtist');
        
        if (track.id) {
            preview?.classList.remove('d-none');
            noSong?.classList.add('d-none');
            
            if (previewImg) previewImg.src = track.image || '';
            if (previewName) previewName.textContent = track.name || '';
            if (previewArtist) previewArtist.textContent = track.artist || '';
        } else {
            preview?.classList.add('d-none');
            noSong?.classList.remove('d-none');
        }
    }

    // Star Rating
    function initializeStarRating() {
        const starContainer = document.getElementById('starRating');
        if (!starContainer) return;
        
        starContainer.addEventListener('click', function(e) {
            const starBtn = e.target.closest('.star');
            if (!starBtn) return;
            
            const rating = parseInt(starBtn.getAttribute('data-value')) || 0;
            setRating(rating);
        });
    }

    function setRating(rating) {
        console.log('Setting rating to:', rating);
        state.selectedRating = rating;
        
        const stars = document.querySelectorAll('#starRating .star');
        stars.forEach((star, index) => {
            const isActive = index < rating;
            star.classList.toggle('active', isActive);
            star.innerHTML = isActive ? 
                '<i class="bi bi-star-fill"></i>' : 
                '<i class="bi bi-star"></i>';
            star.setAttribute('aria-pressed', isActive ? 'true' : 'false');
        });
        
        
        setInputValue('inputRating', rating);
        console.log('Rating hidden field set to:', rating);
        
        updateSubmitButton();
    }

    // Form Handling
    function initializeFormHandlers() {
        const form = document.getElementById('reviewForm');
        if (!form) {
            console.warn('reviewForm not found!');
            return;
        }
        
        console.log('Form handlers initialized');
        
        // Handle form submission
        form.addEventListener('submit', function(e) {
            console.log('=== Form Submit Event Fired ===');
            
            // Log ALL form inputs BEFORE any manipulation
            console.log('RAW FORM STATE (before ensureHiddenFieldsPopulated):');
            const formData = new FormData(form);
            for (let [key, value] of formData.entries()) {
                console.log(`  ${key}: "${value}"`);
            }
            
            // Double-check fields are populated (as a safety net)
            ensureHiddenFieldsPopulated();
            
            // Log AFTER manipulation
            console.log('FORM STATE (after ensureHiddenFieldsPopulated):');
            const formData2 = new FormData(form);
            for (let [key, value] of formData2.entries()) {
                console.log(`  ${key}: "${value}"`);
            }
            
            // Get current values
            const trackId = getInputValue('inputTrackId');
            const rating = getInputValue('inputRating');
            const reviewId = getInputValue('inputReviewId');
            
            // Check if we're editing an existing review
            // A review is being edited if reviewId exists and is a positive number
            const reviewIdNum = parseInt(reviewId);
            const isEditingReview = !isNaN(reviewIdNum) && reviewIdNum > 0;
            
            console.log('Form validation check:', {
                reviewId: reviewId,
                reviewIdNum: reviewIdNum,
                isEditingReview: isEditingReview,
                trackId: trackId || '(MISSING!)',
                trackName: getInputValue('inputTrackName'),
                rating: rating || '(MISSING!)',
                comment: getInputValue('inputComment')
            });
            
            // Only validate track and rating for NEW reviews
            // When editing, the backend will handle validation
            if (!isEditingReview) {
                if (!trackId || !rating || parseInt(rating) === 0) {
                    console.error('VALIDATION FAILED - missing required fields for new review');
                    e.preventDefault();
                    alert('Please select a song and rating before submitting.');
                    return false;
                }
            }
            
            console.log('? Validation passed - form will submit' + (isEditingReview ? ' (editing review #' + reviewIdNum + ')' : ' (new review)'));
        });
    }

    function ensureHiddenFieldsPopulated() {
        console.log('Ensuring hidden fields are populated...');
        
        // Get ReviewId first to know if we're editing
        const reviewId = getInputValue('inputReviewId');
        console.log('ReviewId:', reviewId);
        
        // Check if track fields need population
        let trackId = getInputValue('inputTrackId');
        console.log('Current trackId:', trackId);
        
        if (!trackId) {
            // Try to get from selected track item
            const selected = document.querySelector('.track-item.selected');
            if (selected) {
                console.log('Found selected track, populating fields...');
                trackId = selected.getAttribute('data-track-id') || '';
                setInputValue('inputTrackId', trackId);
                setInputValue('inputTrackName', selected.getAttribute('data-track-name') || '');
                setInputValue('inputArtistName', selected.getAttribute('data-track-artist') || '');
                setInputValue('inputAlbumName', selected.getAttribute('data-track-album') || '');
                setInputValue('inputAlbumImage', selected.getAttribute('data-track-image') || '');
                console.log('Populated trackId:', trackId);
            } else {
                console.warn('No track selected and trackId is empty!');
            }
        }
        
        // Check if rating needs population
        let rating = getInputValue('inputRating');
        console.log('Current rating:', rating);
        
        if (!rating || parseInt(rating) === 0) {
            // Try to get from active stars
            const activeStars = document.querySelectorAll('#starRating .star.active');
            if (activeStars.length > 0) {
                rating = activeStars.length.toString();
                setInputValue('inputRating', rating);
                console.log('Populated rating from stars:', rating);
            } else {
                console.warn('No rating selected!');
            }
        }
        
        // Log final state
        console.log('Final hidden fields:', {
            reviewId: reviewId,
            trackId: getInputValue('inputTrackId'),
            rating: getInputValue('inputRating')
        });
    }

    function updateSubmitButton() {
        const submitBtn = document.getElementById('submitBtn');
        if (!submitBtn) return;
        
        const trackId = getInputValue('inputTrackId');
        const rating = parseInt(getInputValue('inputRating') || '0');
        
        const isValid = trackId && rating > 0;
        submitBtn.classList.toggle('disabled', !isValid);
        submitBtn.setAttribute('aria-disabled', isValid ? 'false' : 'true');
    }

    // Delete Modal
    function initializeDeleteModal() {
        const modal = document.getElementById('deleteModal');
        if (!modal) return;
        
        modal.addEventListener('show.bs.modal', function(event) {
            const button = event.relatedTarget;
            const reviewId = button.getAttribute('data-review-id');
            const reviewTitle = button.getAttribute('data-review-title');
            
            console.log('Delete modal opened for review:', reviewId);
            
            if (!reviewId) {
                console.error('No review ID found');
                return;
            }
            
            // Update modal content
            const titleEl = document.getElementById('deleteReviewTitle');
            if (titleEl) titleEl.textContent = reviewTitle;
            
            // Update form
            const formIdInput = document.getElementById('deleteFormId');
            if (formIdInput) formIdInput.value = reviewId;
            
            const deleteForm = document.getElementById('deleteForm');
            if (deleteForm) deleteForm.action = '/Reviews?handler=Delete';
        });
        
        // Validate before submit
        const deleteForm = document.getElementById('deleteForm');
        if (deleteForm) {
            deleteForm.addEventListener('submit', function(e) {
                const id = document.getElementById('deleteFormId')?.value;
                if (!id) {
                    e.preventDefault();
                    alert('Error: Could not determine review to delete. Please refresh and try again.');
                    return false;
                }
                console.log('Deleting review:', id);
            });
        }
    }

    // Search
    function initializeSearch() {
        const searchInput = document.getElementById('searchInput');
        const searchButton = document.getElementById('searchButton');
        
        if (!searchInput || !searchButton) return;
        
        searchInput.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                performSearch();
            }
        });
        
        searchButton.addEventListener('click', performSearch);
    }

    function performSearch() {
        const searchInput = document.getElementById('searchInput');
        const query = searchInput?.value.trim();
        if (!query) return;
        
        console.log('Performing AJAX search for:', query);
        
        // Show loading indicator
        const loadingIndicator = document.getElementById('searchLoadingIndicator');
        const searchResultsList = document.getElementById('searchResultsList');
        
        if (loadingIndicator) loadingIndicator.style.display = 'block';
        if (searchResultsList) searchResultsList.innerHTML = '';
        
        // Perform AJAX search instead of form submission
        fetch(`/Reviews?handler=SearchTracks&query=${encodeURIComponent(query)}`)
            .then(response => {
                if (!response.ok) {
                    throw new Error('Search failed');
                }
                return response.json();
            })
            .then(tracks => {
                console.log('Search completed, found', tracks.length, 'tracks');
                
                if (loadingIndicator) loadingIndicator.style.display = 'none';
                
                if (tracks.length === 0) {
                    searchResultsList.innerHTML = `
                        <div class="text-center py-4 text-muted">
                            <i class="bi bi-search fs-3 opacity-50 d-block mb-2"></i>
                            <p>No results found for "${escapeHtml(query)}"</p>
                            <small>Try a different search term</small>
                        </div>`;
                } else {
                    // Build results HTML
                    let resultsHtml = `
                        <div class="alert alert-success small mb-2">
                            Found ${tracks.length} results for "${escapeHtml(query)}"
                        </div>`;
                    
                    tracks.forEach(track => {
                        resultsHtml += `
                            <div class="track-item" 
                                 data-track-id="${escapeHtml(track.spotifyId)}" 
                                 data-track-name="${escapeHtml(track.name)}" 
                                 data-track-artist="${escapeHtml(track.artist)}" 
                                 data-track-album="${escapeHtml(track.album)}" 
                                 data-track-image="${escapeHtml(track.imageUrl || '')}"
                                 tabindex="0" role="button">`;
                        
                        if (track.imageUrl) {
                            resultsHtml += `<img src="${escapeHtml(track.imageUrl)}" class="track-img me-3" alt="" />`;
                        } else {
                            resultsHtml += `<div class="track-placeholder me-3"><i class="bi bi-music-note text-white"></i></div>`;
                        }
                        
                        resultsHtml += `
                                <div class="flex-grow-1 overflow-hidden">
                                    <div class="fw-semibold text-truncate">${escapeHtml(track.name)}</div>
                                    <small class="text-muted text-truncate d-block">${escapeHtml(track.artist)}</small>
                                </div>
                            </div>`;
                    });
                    
                    searchResultsList.innerHTML = resultsHtml;
                }
            })
            .catch(error => {
                console.error('Search error:', error);
                if (loadingIndicator) loadingIndicator.style.display = 'none';
                if (searchResultsList) {
                    searchResultsList.innerHTML = `
                        <div class="alert alert-danger">
                            <i class="bi bi-exclamation-triangle me-2"></i>
                            Search failed. Please try again.
                        </div>`;
                }
            });
    }

    // Helper function to escape HTML
    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Initial State
    function loadInitialState() {
        // Load rating from hidden input
        const rating = parseInt(getInputValue('inputRating') || '0');
        const reviewId = getInputValue('inputReviewId');
        const trackId = getInputValue('inputTrackId');
        
        console.log('Loading initial state:', {
            reviewId: reviewId || '(none)',
            trackId: trackId || '(none)',
            rating: rating
        });
        
        // DEBUG: Check actual HTML attributes of hidden inputs
        const reviewIdEl = document.getElementById('inputReviewId');
        const trackIdEl = document.getElementById('inputTrackId');
        const ratingEl = document.getElementById('inputRating');
        
        console.log('Hidden input HTML attributes:');
        if (reviewIdEl) {
            console.log('  ReviewId input:', {
                id: reviewIdEl.id,
                name: reviewIdEl.name,
                value: reviewIdEl.value,
                type: reviewIdEl.type
            });
        } else {
            console.error('  ReviewId input NOT FOUND!');
        }
        
        if (trackIdEl) {
            console.log('  TrackId input:', {
                id: trackIdEl.id,
                name: trackIdEl.name,
                value: trackIdEl.value,
                type: trackIdEl.type
            });
        } else {
            console.error('  TrackId input NOT FOUND!');
        }
        
        if (ratingEl) {
            console.log('  Rating input:', {
                id: ratingEl.id,
                name: ratingEl.name,
                value: ratingEl.value,
                type: ratingEl.type
            });
        } else {
            console.error('  Rating input NOT FOUND!');
        }
        
        if (rating > 0) {
            setRating(rating);
        }
        
        // Pre-select track if editing
        if (trackId) {
            const trackItem = document.querySelector(`[data-track-id="${trackId}"]`);
            if (trackItem) {
                trackItem.classList.add('selected');
                trackItem.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
                console.log('Pre-selected track for editing:', trackId);
            } else {
                console.warn('Track item not found in DOM for trackId:', trackId);
            }
        }
        
        // Update submit button state
        updateSubmitButton();
    }

    // Utility Functions
    function getInputValue(id) {
        const el = document.getElementById(id);
        return el ? el.value : '';
    }

    function setInputValue(id, value) {
        const el = document.getElementById(id);
        if (el) el.value = value;
    }

    function logFormData() {
        console.log('Form Data:', {
            reviewId: getInputValue('inputReviewId'),
            trackId: getInputValue('inputTrackId'),
            trackName: getInputValue('inputTrackName'),
            artist: getInputValue('inputArtistName'),
            rating: getInputValue('inputRating'),
            comment: document.getElementById('inputComment')?.value
        });
    }
})();
