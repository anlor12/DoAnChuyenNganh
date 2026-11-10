/**
 * Decart AI Real-time Virtual Try-On (Lucy V-TON 3.6 over WebRTC)
 * Thiết kế tối giản, sang trọng chuẩn Anywear cho Khách hàng
 */

import { createDecartClient, models } from "https://esm.sh/@decartai/sdk";

let decartRealtimeClient = null;
let localMediaStream = null;
let currentFacingMode = 'user';
let vtonTimerInterval = null;
let cachedDecartApiKey = null;

let currentVtonProduct = {
    id: 0,
    name: 'Sản phẩm đang chọn',
    price: '',
    imageUrl: '',
    category: 'upper_body'
};

/**
 * Lấy Decart API Key từ cấu hình hệ thống (Admin) một cách tự động
 */
async function fetchDecartApiKey() {
    if (cachedDecartApiKey) return cachedDecartApiKey;
    
    const localKey = localStorage.getItem('DECART_API_KEY');
    if (localKey && localKey.trim()) {
        cachedDecartApiKey = localKey.trim();
        return cachedDecartApiKey;
    }

    try {
        const res = await fetch('/api/Ai/DecartKey');
        if (res.ok) {
            const data = await res.json();
            if (data && data.key) {
                cachedDecartApiKey = data.key;
                localStorage.setItem('DECART_API_KEY', data.key);
                return cachedDecartApiKey;
            }
        }
    } catch (e) {
        console.warn('Không thể nạp Decart API Key từ backend:', e);
    }

    cachedDecartApiKey = "dct_test_EoXNbcuxWjeBFMaspizrmUwHXhbNDrOwOizJUoXLbdesVsagespbmGdvgcpxygqs";
    return cachedDecartApiKey;
}

/**
 * Chuyển đổi URL ảnh hoặc Base64 thành Blob cho Decart SDK
 */
async function getGarmentBlob(imageUrl) {
    if (!imageUrl) return null;
    if (imageUrl instanceof Blob) return imageUrl;
    try {
        const res = await fetch(imageUrl);
        return await res.blob();
    } catch (e) {
        console.warn('Không thể fetch ảnh thành Blob:', e);
        return null;
    }
}

/**
 * Mở modal thử đồ ảo
 */
window.openVtonModal = function (productData) {
    if (productData) {
        currentVtonProduct = Object.assign(currentVtonProduct, productData);
    } else {
        const prodNameEl = document.querySelector('h1, .product-title, .sticky-buy-name');
        const prodPriceEl = document.querySelector('.product-price, .current-price, .sticky-buy-price');
        const prodImgEl = document.querySelector('#product2DImage, .main-product-image, #mainProductImg');

        if (prodNameEl) currentVtonProduct.name = prodNameEl.innerText.trim();
        if (prodPriceEl) currentVtonProduct.price = prodPriceEl.innerText.trim();
        if (prodImgEl && prodImgEl.src) currentVtonProduct.imageUrl = prodImgEl.src;
    }

    updateProductCardUI();

    const modalEl = document.getElementById('vtonModal');
    if (modalEl) {
        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();
        loadQuickWardrobe();
        fetchDecartApiKey();
    }
};

/**
 * Cập nhật thông tin thẻ sản phẩm đang chọn
 */
function updateProductCardUI() {
    const modalNameEl = document.getElementById('vtonProductName');
    const modalPriceEl = document.getElementById('vtonProductPrice');
    const modalImgEl = document.getElementById('vtonProductImg');
    const modalMetaEl = document.getElementById('vtonProductMeta');

    if (modalNameEl) modalNameEl.innerText = currentVtonProduct.name || 'Sản phẩm đang chọn';
    if (modalPriceEl) modalPriceEl.innerText = currentVtonProduct.price || '';
    if (modalImgEl && currentVtonProduct.imageUrl) modalImgEl.src = currentVtonProduct.imageUrl;

    const selectedSize = document.querySelector('.variant-option.selected')?.innerText || 'M';
    const selectedColor = document.querySelector('.color-option.selected, .color-dot.selected')?.title || 'Tiêu chuẩn';
    if (modalMetaEl) modalMetaEl.innerText = `Size: ${selectedSize} | Màu: ${selectedColor}`;
}

/**
 * Nạp danh sách Tủ đồ đổi nhanh từ Database (Dạng Grid 2 cột trong ngăn kéo phải)
 */
async function loadQuickWardrobe() {
    const listEl = document.getElementById('vtonQuickWardrobeList');
    if (!listEl) return;

    try {
        const res = await fetch('/api/Ai/QuickWardrobe');
        if (!res.ok) throw new Error('Không thể nạp danh sách sản phẩm');
        const items = await res.json();

        if (items && items.length > 0) {
            listEl.innerHTML = '';
            items.forEach(item => {
                const card = document.createElement('div');
                card.className = 'vton-wardrobe-card';
                card.title = `Ướm thử: ${item.name}`;
                card.innerHTML = `
                    <div class="rounded-1 overflow-hidden mb-1" style="aspect-ratio: 1; background: #ffffff;">
                        <img src="${item.imageUrl}" alt="${item.name}" class="w-100 h-100 object-fit-cover" onerror="this.src='/images/no-image.png'" />
                    </div>
                    <div class="text-dark small fw-bold text-truncate" style="font-size: 11px; line-height: 1.3;">${item.name}</div>
                    <div class="fw-bold mt-1" style="font-size: 11px; color: #e0001b;">${item.price}</div>
                `;
                card.onclick = () => {
                    document.querySelectorAll('.vton-wardrobe-card').forEach(c => c.classList.remove('active'));
                    card.classList.add('active');
                    window.switchDecartGarment(item);
                };
                listEl.appendChild(card);
            });
        }
    } catch (err) {
        console.warn('Lỗi nạp tủ đồ:', err);
    }
}

/**
 * ĐỔI TRANG PHỤC TRỰC TIẾP TRÊN LUỒNG WEBRTC CỦA DECART AI
 */
window.switchDecartGarment = async function (item) {
    currentVtonProduct = {
        id: item.id || 0,
        name: item.name || 'Trang phục mới',
        price: item.price || '',
        imageUrl: item.imageUrl || '',
        category: item.category || 'upper_body'
    };

    updateProductCardUI();

    if (decartRealtimeClient) {
        try {
            console.log('Đang đổi trang phục qua Decart WebRTC:', currentVtonProduct.name);
            const imageBlob = await getGarmentBlob(currentVtonProduct.imageUrl);
            
            const payload = {
                prompt: {
                    text: `Wearing ${currentVtonProduct.name} with natural fitting`,
                    enhance: true
                }
            };
            if (imageBlob) {
                payload.image = new File([imageBlob], "garment.jpg", { type: imageBlob.type || "image/jpeg" });
            }

            await decartRealtimeClient.set(payload);
            console.log('Đổi trang phục thành công trên WebRTC!');
        } catch (err) {
            console.warn('Lỗi khi đổi trang phục trên WebRTC stream:', err);
        }
    }
};

/**
 * Tải ảnh trang phục từ máy tính
 */
window.uploadCustomGarmentForDecart = function (input) {
    if (input.files && input.files[0]) {
        const file = input.files[0];
        const reader = new FileReader();
        reader.onload = function (e) {
            window.switchDecartGarment({
                id: 0,
                name: file.name.replace(/\.[^/.]+$/, ''),
                price: 'Tùy chỉnh',
                imageUrl: e.target.result,
                category: 'upper_body'
            });
        };
        reader.readAsDataURL(file);
    }
};

/**
 * KÍCH HOẠT PHIÊN THỬ ĐỒ DECART AI LUCY-VTON THỜI GIAN THỰC (WEBRTC)
 */
window.startDecartVtonSession = async function () {
    const apiKey = await fetchDecartApiKey();
    if (!apiKey) {
        alert('Hệ thống chưa được thiết lập Decart API Key. Vui lòng liên hệ Quản trị viên!');
        return;
    }

    const placeholder = document.getElementById('vtonStreamPlaceholder');
    const loadingOverlay = document.getElementById('vtonLoadingOverlay');
    const loadingTitle = document.getElementById('vtonLoadingTitle');
    const localVideo = document.getElementById('vtonLocalVideo');
    const aiVideo = document.getElementById('vtonAiVideo');
    const statusBadge = document.getElementById('vtonStatusBadge');
    const statusText = document.getElementById('vtonStatusText');
    const statusDot = document.getElementById('vtonStatusDot');
    const btnStop = document.getElementById('btnStopStream');
    const timerBadge = document.getElementById('vtonTimerBadge');
    const timerSec = document.getElementById('vtonTimerSec');

    try {
        if (placeholder) placeholder.classList.add('d-none');
        if (loadingOverlay) loadingOverlay.classList.remove('d-none');
        if (statusBadge) statusBadge.classList.remove('d-none');
        if (statusText) statusText.innerText = 'Đang mở webcam...';

        // 1. Khởi tạo model Lucy V-TON từ Decart SDK
        const model = models.realtime("lucy-vton-3.6");

        // 2. Mở Webcam của người dùng
        localMediaStream = await navigator.mediaDevices.getUserMedia({
            audio: false,
            video: {
                facingMode: currentFacingMode,
                width: model.width || 720,
                height: model.height || 720,
                frameRate: model.fps || 24
            }
        });

        if (localVideo) {
            localVideo.srcObject = localMediaStream;
            localVideo.classList.remove('d-none');
            await localVideo.play();
        }

        if (statusText) statusText.innerText = 'Đang kết nối WebRTC tới Decart GPU...';
        if (loadingTitle) loadingTitle.innerText = 'Đang kết nối Lucy V-TON Engine...';

        const garmentBlob = await getGarmentBlob(currentVtonProduct.imageUrl);

        // 3. Khởi tạo Decart Client
        const client = createDecartClient({ apiKey });

        const connectConfig = {
            model,
            mirror: "auto",
            onRemoteStream: (transformedStream) => {
                console.log('Nhận được luồng video AI từ Decart WebRTC!');
                if (aiVideo) {
                    aiVideo.srcObject = transformedStream;
                    aiVideo.classList.remove('d-none');
                }
                if (localVideo) localVideo.classList.add('d-none');
                if (loadingOverlay) loadingOverlay.classList.add('d-none');

                if (statusDot) {
                    statusDot.className = 'spinner-grow spinner-grow-sm text-success me-1';
                }
                if (statusText) statusText.innerText = 'Live AI VTON (30 FPS)';
                if (btnStop) btnStop.classList.remove('d-none');
            },
            initialState: {
                prompt: {
                    text: `Wearing ${currentVtonProduct.name} with natural fitting`,
                    enhance: true
                }
            }
        };

        if (garmentBlob) {
            connectConfig.initialState.image = new File([garmentBlob], "garment.jpg", { type: garmentBlob.type || "image/jpeg" });
        }

        // 4. Kết nối WebRTC Realtime Stream
        decartRealtimeClient = await client.realtime.connect(localMediaStream, connectConfig);

        // 5. Bắt đầu đếm ngược Timer 60s
        if (timerBadge) timerBadge.classList.remove('d-none');
        let remainingSeconds = 60;
        if (timerSec) timerSec.innerText = remainingSeconds;

        clearInterval(vtonTimerInterval);
        vtonTimerInterval = setInterval(() => {
            remainingSeconds--;
            if (timerSec) timerSec.innerText = remainingSeconds;

            if (remainingSeconds <= 0) {
                clearInterval(vtonTimerInterval);
                if (timerBadge) timerBadge.classList.add('d-none');
                alert('Phiên thử đồ thời gian thực (60s) đã kết thúc để tối ưu tài nguyên.');
                window.stopDecartVtonSession();
            }
        }, 1000);

    } catch (err) {
        console.error('Lỗi khởi tạo phiên Decart AI:', err);
        if (loadingOverlay) loadingOverlay.classList.add('d-none');
        if (placeholder) placeholder.classList.remove('d-none');
        if (statusBadge) statusBadge.classList.add('d-none');

        alert('Lỗi kết nối Decart AI WebRTC: ' + (err.message || 'Kiểm tra lại kết nối mạng!'));
    }
};

/**
 * Dừng phiên thử đồ WebRTC
 */
window.stopDecartVtonSession = function () {
    if (decartRealtimeClient) {
        try {
            decartRealtimeClient.disconnect();
        } catch (e) {
            console.warn(e);
        }
        decartRealtimeClient = null;
    }

    if (localMediaStream) {
        localMediaStream.getTracks().forEach(t => t.stop());
        localMediaStream = null;
    }

    const aiVideo = document.getElementById('vtonAiVideo');
    const localVideo = document.getElementById('vtonLocalVideo');
    const placeholder = document.getElementById('vtonStreamPlaceholder');
    const btnStop = document.getElementById('btnStopStream');
    const statusBadge = document.getElementById('vtonStatusBadge');
    const timerBadge = document.getElementById('vtonTimerBadge');

    if (aiVideo) {
        aiVideo.srcObject = null;
        aiVideo.classList.add('d-none');
    }
    if (localVideo) {
        localVideo.srcObject = null;
        localVideo.classList.add('d-none');
    }
    if (placeholder) placeholder.classList.remove('d-none');
    if (btnStop) btnStop.classList.add('d-none');
    if (statusBadge) statusBadge.classList.add('d-none');
    if (timerBadge) timerBadge.classList.add('d-none');

    clearInterval(vtonTimerInterval);
};

/**
 * Đổi camera trước / sau
 */
window.toggleDecartCameraFacing = function () {
    currentFacingMode = (currentFacingMode === 'user') ? 'environment' : 'user';
    window.stopDecartVtonSession();
    window.startDecartVtonSession();
};

/**
 * Chụp ảnh snapshot từ luồng video AI đang chạy
 */
window.takeSnapshot = function () {
    const aiVideo = document.getElementById('vtonAiVideo');
    const localVideo = document.getElementById('vtonLocalVideo');
    const activeVideo = (!aiVideo.classList.contains('d-none') && aiVideo.videoWidth) ? aiVideo : localVideo;

    if (!activeVideo || !activeVideo.videoWidth) {
        alert('Chưa có luồng video đang chạy để chụp ảnh!');
        return;
    }

    const canvas = document.getElementById('vtonCanvas');
    canvas.width = activeVideo.videoWidth;
    canvas.height = activeVideo.videoHeight;
    const ctx = canvas.getContext('2d');
    ctx.drawImage(activeVideo, 0, 0, canvas.width, canvas.height);

    const base64 = canvas.toDataURL('image/jpeg', 0.9);

    const snapshotImg = document.getElementById('vtonSnapshotImg');
    const btnBack = document.getElementById('btnBackToLive');
    const btnDownload = document.getElementById('vtonDownloadImgBtn');

    if (snapshotImg) {
        snapshotImg.src = base64;
        snapshotImg.classList.remove('d-none');
    }
    activeVideo.classList.add('d-none');
    if (btnBack) btnBack.classList.remove('d-none');
    if (btnDownload) btnDownload.disabled = false;

    const historyList = document.getElementById('vtonHistoryList');
    if (historyList) {
        if (historyList.querySelector('.fst-italic')) historyList.innerHTML = '';
        const thumb = document.createElement('img');
        thumb.src = base64;
        thumb.className = 'rounded-1 border object-fit-cover cursor-pointer shadow-sm';
        thumb.style.width = '48px';
        thumb.style.height = '48px';
        thumb.style.borderColor = '#e5e5e5';
        thumb.onclick = () => {
            snapshotImg.src = base64;
            snapshotImg.classList.remove('d-none');
            activeVideo.classList.add('d-none');
            if (btnBack) btnBack.classList.remove('d-none');
        };
        historyList.prepend(thumb);
    }
};

window.backToLiveVideo = function () {
    const snapshotImg = document.getElementById('vtonSnapshotImg');
    const aiVideo = document.getElementById('vtonAiVideo');
    const localVideo = document.getElementById('vtonLocalVideo');
    const btnBack = document.getElementById('btnBackToLive');

    if (snapshotImg) snapshotImg.classList.add('d-none');
    if (aiVideo && aiVideo.srcObject) aiVideo.classList.remove('d-none');
    else if (localVideo && localVideo.srcObject) localVideo.classList.remove('d-none');
    if (btnBack) btnBack.classList.add('d-none');
};

window.downloadSnapshot = function () {
    const snapshotImg = document.getElementById('vtonSnapshotImg');
    if (snapshotImg && snapshotImg.src) {
        const a = document.createElement('a');
        a.href = snapshotImg.src;
        a.download = `VTON-${(currentVtonProduct.name || 'outfit').replace(/\s+/g, '_')}.jpg`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
    }
};

window.addVtonProductToCart = function () {
    const mainAddToCartBtn = document.getElementById('addToCartBtn');
    if (mainAddToCartBtn) {
        mainAddToCartBtn.click();
        const modalEl = document.getElementById('vtonModal');
        if (modalEl) {
            bootstrap.Modal.getInstance(modalEl)?.hide();
            window.stopDecartVtonSession();
        }
    } else {
        alert('Đã thêm sản phẩm ' + currentVtonProduct.name + ' vào giỏ hàng thành công!');
    }
};
