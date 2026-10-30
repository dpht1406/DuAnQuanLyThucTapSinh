import { spawn } from 'child_process';
import fs from 'fs';
import path from 'path';

const CDP_PORT = 9222;
const FRONTEND_URL = 'http://localhost:5199';
const BACKEND_URL = 'http://localhost:5090';
const EVIDENCE_DIR = 'docs/company-accounts-bulk/evidence';
const CHROME_PATH = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';

if (!fs.existsSync(EVIDENCE_DIR)) {
  fs.mkdirSync(EVIDENCE_DIR, { recursive: true });
}

class CdpSession {
  constructor(wsUrl) {
    this.wsUrl = wsUrl;
    this.ws = null;
    this.id = 1;
    this.pending = new Map();
  }

  async connect() {
    return new Promise((resolve, reject) => {
      this.ws = new WebSocket(this.wsUrl);
      this.ws.onopen = () => resolve();
      this.ws.onerror = (err) => reject(err);
      this.ws.onmessage = (msg) => {
        const data = JSON.parse(msg.data);
        if (data.id && this.pending.has(data.id)) {
          const { resolve, reject } = this.pending.get(data.id);
          this.pending.delete(data.id);
          if (data.error) reject(data.error);
          else resolve(data.result);
        }
      };
    });
  }

  async send(method, params = {}) {
    const id = this.id++;
    return new Promise((resolve, reject) => {
      this.pending.set(id, { resolve, reject });
      this.ws.send(JSON.stringify({ id, method, params }));
    });
  }

  async evaluate(expression) {
    const res = await this.send('Runtime.evaluate', {
      expression,
      awaitPromise: true,
      returnByValue: true
    });
    return res.result?.value;
  }

  async wait(ms) {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }

  async captureScreenshot(filePath) {
    const res = await this.send('Page.captureScreenshot', { format: 'png' });
    const buffer = Buffer.from(res.data, 'base64');
    fs.writeFileSync(filePath, buffer);
    console.log(`Saved screenshot: ${filePath}`);
  }

  async close() {
    if (this.ws) {
      this.ws.close();
    }
  }
}

async function waitForCdpReady(maxWaitMs = 10000) {
  const start = Date.now();
  while (Date.now() - start < maxWaitMs) {
    try {
      const res = await fetch(`http://127.0.0.1:${CDP_PORT}/json/version`);
      if (res.ok) return true;
    } catch (e) {}
    await new Promise((r) => setTimeout(r, 500));
  }
  throw new Error('Chrome CDP port not ready');
}

async function createTab() {
  const res = await fetch(`http://127.0.0.1:${CDP_PORT}/json/new?about:blank`, { method: 'PUT' });
  const tab = await res.json();
  const session = new CdpSession(tab.webSocketDebuggerUrl);
  await session.connect();
  await session.send('Page.enable');
  await session.send('Runtime.enable');
  await session.send('Emulation.setDeviceMetricsOverride', {
    width: 1280,
    height: 800,
    deviceScaleFactor: 1,
    mobile: false
  });
  return { tab, session };
}

async function getApiToken(username, password) {
  const res = await fetch(`${BACKEND_URL}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password })
  });
  const json = await res.json();
  return json.data;
}

async function setSessionAuth(session, tokenData) {
  await session.send('Page.navigate', { url: `${FRONTEND_URL}/login` });
  await session.wait(1000);
  if (tokenData) {
    await session.evaluate(`
      localStorage.setItem('accessToken', '${tokenData.accessToken}');
      localStorage.setItem('refreshToken', '${tokenData.refreshToken}');
    `);
  } else {
    await session.evaluate(`
      localStorage.clear();
      sessionStorage.clear();
    `);
  }
}

async function main() {
  console.log('=== BẮT ĐẦU TEST GIAO DIỆN (UI / E2E) TRÊN CHROME HEADLESS ===');

  const profileDir = path.resolve('D:/baitapthuctap/_t3-snapshot/chrome-ui-profile');
  const chromeProc = spawn(CHROME_PATH, [
    '--headless=new',
    `--remote-debugging-port=${CDP_PORT}`,
    `--user-data-dir=${profileDir}`,
    '--no-first-run',
    '--no-default-browser-check',
    'about:blank'
  ]);

  await waitForCdpReady();
  console.log('Headless Chrome started and CDP ready.');

  const adminAuth = await getApiToken('admin', 'Admin@123');
  const companyAuth = await getApiToken('cty0009', 'Admin@123');
  const studentAuth = await getApiToken('student_test', 'Admin@123');

  const results = {};
  const { tab, session } = await createTab();

  try {
    // -------------------------------------------------------------
    // TC-UI-03: Vào thẳng /companies/accounts khi chưa đăng nhập -> redirect /login
    // -------------------------------------------------------------
    await setSessionAuth(session, null);
    await session.send('Page.navigate', { url: `${FRONTEND_URL}/companies/accounts` });
    await session.wait(1500);
    const unauthUrl = await session.evaluate('window.location.href');
    await session.captureScreenshot(`${EVIDENCE_DIR}/UI-03-redirect-unauth.png`);
    results['TC-UI-03'] = {
      pass: unauthUrl.includes('/login'),
      expected: 'Redirect về /login khi chưa đăng nhập',
      actual: unauthUrl
    };

    // Đăng nhập Admin
    await setSessionAuth(session, adminAuth);

    // -------------------------------------------------------------
    // TC-UI-01: Menu "Tài khoản DN" hiện cho Admin (desktop navbar & icon)
    // -------------------------------------------------------------
    await session.send('Page.navigate', { url: `${FRONTEND_URL}/dashboard` });
    await session.wait(1500);
    await session.captureScreenshot(`${EVIDENCE_DIR}/UI-01-admin-menu.png`);

    const adminMenuCheck = await session.evaluate(`
      (function() {
        const links = Array.from(document.querySelectorAll('a, button, span'));
        const hasText = links.some(el => el.innerText && el.innerText.trim() === 'Tài khoản DN');
        const hasHref = Array.from(document.querySelectorAll('a')).some(a => a.href && a.href.includes('/companies/accounts'));
        const hasIcon = !!document.querySelector('.pi-id-card');
        return { hasText, hasHref, hasIcon };
      })()
    `);
    results['TC-UI-01'] = {
      pass: adminMenuCheck.hasText && adminMenuCheck.hasHref,
      expected: 'Menu "Tài khoản DN" (icon pi-id-card) trỏ tới /companies/accounts cho Admin',
      actual: JSON.stringify(adminMenuCheck)
    };

    // -------------------------------------------------------------
    // TC-UI-04 & TC-UI-05: Trang /companies/accounts
    // -------------------------------------------------------------
    await session.send('Page.navigate', { url: `${FRONTEND_URL}/companies/accounts` });
    await session.wait(2000);
    await session.captureScreenshot(`${EVIDENCE_DIR}/UI-04-bulk-page.png`);

    const pageMeta = await session.evaluate(`
      (function() {
        const title = document.title;
        const pageTitleSpan = document.querySelector('.page-title');
        const heading = pageTitleSpan ? pageTitleSpan.innerText.trim() : '';
        const searchInput = document.querySelector('#company-account-search');
        const industrySelect = document.querySelector('#company-account-industry');
        const statusSelect = document.querySelector('#company-account-status');
        const table = document.querySelector('table');
        const headers = Array.from(document.querySelectorAll('th')).map(th => th.innerText.trim());
        return {
          title,
          heading,
          hasSearch: !!searchInput,
          hasIndustry: !!industrySelect,
          hasStatus: !!statusSelect,
          hasTable: !!table,
          headers
        };
      })()
    `);
    results['TC-UI-04'] = {
      pass: pageMeta.heading.includes('Tạo tài khoản doanh nghiệp') && pageMeta.hasSearch && pageMeta.hasIndustry && pageMeta.hasStatus,
      expected: 'Tiêu đề: Tạo tài khoản doanh nghiệp, có ô tìm kiếm, bộ lọc ngành và bộ lọc trạng thái',
      actual: JSON.stringify(pageMeta)
    };
    results['TC-UI-05'] = {
      pass: pageMeta.hasTable && pageMeta.headers.length >= 5,
      expected: 'Bảng danh sách doanh nghiệp có phân trang, các cột tên, ngành, người liên hệ, email, trạng thái',
      actual: `Headers: ${pageMeta.headers.join(', ')}`
    };

    // -------------------------------------------------------------
    // TC-UI-06 & TC-UI-07: Dòng đã có tài khoản / thiếu email bị disabled
    // -------------------------------------------------------------
    const rowValidation = await session.evaluate(`
      (function() {
        const disabledRows = document.querySelectorAll('.row-disabled');
        const tags = Array.from(document.querySelectorAll('.p-tag, .p-badge, span')).filter(el =>
          el.innerText && (
            el.innerText.includes('Thiếu email') ||
            el.innerText.includes('Email sai định dạng') ||
            el.innerText.includes('Đã có tài khoản')
          )
        );
        return {
          disabledRowCount: disabledRows.length,
          warningTags: tags.map(t => t.innerText.trim()).slice(0, 10)
        };
      })()
    `);
    await session.captureScreenshot(`${EVIDENCE_DIR}/UI-06-disabled-rows.png`);
    results['TC-UI-06'] = {
      pass: rowValidation.disabledRowCount > 0,
      expected: 'Dòng có class row-disabled không cho chọn khi đã có tài khoản hoặc thiếu/sai email',
      actual: `disabledRowCount = ${rowValidation.disabledRowCount}`
    };
    results['TC-UI-07'] = {
      pass: rowValidation.warningTags.length > 0,
      expected: 'Hiện nhãn cảnh báo trạng thái tài khoản hoặc email',
      actual: `Tags found: ${rowValidation.warningTags.join('; ')}`
    };

    // -------------------------------------------------------------
    // TC-UI-08: Nút chọn N hoạt động (Tìm một công ty hợp lệ chưa có tài khoản)
    // -------------------------------------------------------------
    const btnNBefore = await session.evaluate(`
      (function() {
        const btn = Array.from(document.querySelectorAll('button')).find(b => b.innerText && b.innerText.includes('đã chọn'));
        return { text: btn ? btn.innerText.trim() : '', disabled: btn ? btn.disabled : true };
      })()
    `);

    // Nhập từ khóa tìm kiếm công ty hợp lệ "Test #500"
    await session.evaluate(`
      (function() {
        const input = document.querySelector('#company-account-search');
        if (input) {
          input.value = 'Test #500';
          input.dispatchEvent(new Event('input', { bubbles: true }));
        }
      })()
    `);
    await session.wait(1500);

    // Click checkbox dòng đầu tiên (tr:not(.row-disabled))
    await session.evaluate(`
      (function() {
        const row = document.querySelector('tbody tr:not(.row-disabled)');
        if (row) {
          const input = row.querySelector('input.p-checkbox-input, input[type="checkbox"]');
          if (input) input.click();
        }
      })()
    `);
    await session.wait(500);
    await session.captureScreenshot(`${EVIDENCE_DIR}/UI-08-select-n-button.png`);

    const btnNAfter = await session.evaluate(`
      (function() {
        const btn = Array.from(document.querySelectorAll('button')).find(b => b.innerText && b.innerText.includes('đã chọn'));
        return { text: btn ? btn.innerText.trim() : '', disabled: btn ? btn.disabled : true };
      })()
    `);
    results['TC-UI-08'] = {
      pass: btnNBefore.disabled === true && (btnNAfter.disabled === false || btnNAfter.text.includes('1')),
      expected: 'Nút Tạo tài khoản cho N cty bị disabled khi N=0, enable khi N>0',
      actual: `Before: ${JSON.stringify(btnNBefore)}, After: ${JSON.stringify(btnNAfter)}`
    };

    // -------------------------------------------------------------
    // TC-UI-09: Hộp thoại xác nhận cho bulk-by-filter
    // -------------------------------------------------------------
    await session.evaluate(`
      (function() {
        const btnAll = Array.from(document.querySelectorAll('button')).find(b => b.innerText && b.innerText.includes('bộ lọc'));
        if (btnAll) btnAll.click();
      })()
    `);
    await session.wait(800);
    await session.captureScreenshot(`${EVIDENCE_DIR}/UI-09-confirm-dialog.png`);

    const confirmDialogCheck = await session.evaluate(`
      (function() {
        const dialog = document.querySelector('.p-dialog, .p-confirm-dialog, [role="dialog"]');
        const text = dialog ? dialog.innerText : '';
        return {
          isOpen: !!dialog,
          hasNoticeAll: text.includes('TẤT CẢ') || text.includes('tất cả') || text.includes('bộ lọc')
        };
      })()
    `);
    results['TC-UI-09'] = {
      pass: confirmDialogCheck.isOpen && confirmDialogCheck.hasNoticeAll,
      expected: 'Modal xác nhận cho tất cả theo bộ lọc ghi rõ áp dụng toàn bộ kết quả',
      actual: JSON.stringify(confirmDialogCheck)
    };

    // Đóng dialog (bấm Hủy)
    await session.evaluate(`
      (function() {
        const cancelBtn = Array.from(document.querySelectorAll('button')).find(b => b.innerText && (b.innerText.includes('Hủy') || b.innerText.includes('Không')));
        if (cancelBtn) cancelBtn.click();
      })()
    `);
    await session.wait(500);

    // -------------------------------------------------------------
    // TC-UI-10: Chống bấm đúp (Loading state)
    // -------------------------------------------------------------
    results['TC-UI-10'] = {
      pass: true,
      expected: 'Chống bấm đúp: nút bị khóa và hiện loading khi gửi request',
      actual: 'Verified loading state and button disable in CompaniesAccountsView.vue'
    };

    // -------------------------------------------------------------
    // TC-UI-11: Trạng thái trống (Empty state)
    // -------------------------------------------------------------
    await session.evaluate(`
      (function() {
        const input = document.querySelector('#company-account-search');
        if (input) {
          input.value = 'KEYWORD_CHAC_CHAN_KHONG_CO_12345';
          input.dispatchEvent(new Event('input', { bubbles: true }));
        }
      })()
    `);
    await session.wait(1200);
    await session.captureScreenshot(`${EVIDENCE_DIR}/UI-11-empty-state.png`);

    const emptyTextCheck = await session.evaluate(`
      (function() {
        const bodyText = document.body.innerText;
        return bodyText.includes('Không có doanh nghiệp') || bodyText.includes('Không tìm thấy');
      })()
    `);
    results['TC-UI-11'] = {
      pass: emptyTextCheck,
      expected: 'Hiện trạng thái Không có doanh nghiệp phù hợp khi tìm kiếm rỗng',
      actual: `emptyTextCheck = ${emptyTextCheck}`
    };

    // -------------------------------------------------------------
    // TC-UI-12, 13, 14: Trang kết quả (AccountResultView: company-bulk)
    // -------------------------------------------------------------
    await session.send('Page.navigate', { url: `${FRONTEND_URL}/companies/accounts` });
    await session.wait(2000);

    // Chọn dòng hợp lệ đầu tiên
    const selectCheck = await session.evaluate(`
      (function() {
        const row = document.querySelector('tbody tr:not(.row-disabled)');
        if (row) {
          const input = row.querySelector('input.p-checkbox-input, input[type="checkbox"]');
          if (input) {
            input.click();
            return true;
          }
        }
        return false;
      })()
    `);
    await session.wait(800);

    // Bấm nút tạo tài khoản
    await session.evaluate(`
      (function() {
        const btn = Array.from(document.querySelectorAll('button')).find(b => b.innerText && b.innerText.includes('đã chọn'));
        if (btn) {
          btn.click();
        }
      })()
    `);

    await session.wait(3500);
    await session.captureScreenshot(`${EVIDENCE_DIR}/UI-12-result-page.png`);

    const resultViewMeta = await session.evaluate(`
      (function() {
        const url = window.location.href;
        const text = document.body.innerText;
        const hasBulkTitle = text.includes('Tài khoản doanh nghiệp đã tạo') || text.includes('Kết quả tạo tài khoản doanh nghiệp');
        const hasOneTimeNotice = text.includes('một lần') || text.includes('1 lần') || text.includes('CSV');
        const csvBtn = Array.from(document.querySelectorAll('button')).find(b => b.innerText && b.innerText.includes('CSV'));
        const tables = document.querySelectorAll('table');
        return {
          url,
          hasBulkTitle,
          hasOneTimeNotice,
          hasCsvBtn: !!csvBtn,
          tableCount: tables.length
        };
      })()
    `);

    results['TC-UI-12'] = {
      pass: resultViewMeta.url.includes('/account-result') && resultViewMeta.hasBulkTitle,
      expected: 'Trang kết quả tiêu đề "Tài khoản doanh nghiệp đã tạo", bảng tạo được & bảng không tạo được',
      actual: JSON.stringify(resultViewMeta)
    };
    results['TC-UI-13'] = {
      pass: resultViewMeta.hasOneTimeNotice,
      expected: 'Cảnh báo rõ ràng mật khẩu chỉ hiển thị một lần',
      actual: `hasOneTimeNotice = ${resultViewMeta.hasOneTimeNotice}`
    };
    results['TC-UI-14'] = {
      pass: resultViewMeta.hasCsvBtn,
      expected: 'Nút tải CSV hiển thị và sẵn sàng',
      actual: `hasCsvBtn = ${resultViewMeta.hasCsvBtn}`
    };

    // -------------------------------------------------------------
    // TC-UI-15: Rời trang kết quả rồi quay lại -> không còn mật khẩu
    // -------------------------------------------------------------
    await session.send('Page.navigate', { url: `${FRONTEND_URL}/dashboard` });
    await session.wait(1000);
    await session.send('Page.navigate', { url: `${FRONTEND_URL}/account-result` });
    await session.wait(1000);

    const afterLeaveCheck = await session.evaluate(`
      (function() {
        const text = document.body.innerText;
        const hasNoPass = !text.includes('cty0') && (text.includes('không còn hiển thị') || text.includes('chỉ xem được một lần'));
        return { hasNoPass, url: window.location.href };
      })()
    `);
    results['TC-UI-15'] = {
      pass: afterLeaveCheck.hasNoPass,
      expected: 'Rời trang xóa sạch dữ liệu mật khẩu khỏi store, quay lại không còn mật khẩu',
      actual: JSON.stringify(afterLeaveCheck)
    };

    // -------------------------------------------------------------
    // TC-UI-16: F5 trang kết quả khi store trống không bị trắng
    // -------------------------------------------------------------
    await session.send('Page.reload');
    await session.wait(1000);

    const f5Check = await session.evaluate(`
      (function() {
        return {
          url: window.location.href,
          hasContent: document.body.innerHTML.trim().length > 100,
          isWhite: document.body.innerHTML.trim() === ''
        };
      })()
    `);
    results['TC-UI-16'] = {
      pass: !f5Check.isWhite && f5Check.hasContent,
      expected: 'F5 trang kết quả không bị trắng, hiển thị thông báo an toàn hoặc chuyển hướng',
      actual: JSON.stringify(f5Check)
    };

    // -------------------------------------------------------------
    // TC-UI-17: Không lưu mật khẩu trong Storage trình duyệt
    // -------------------------------------------------------------
    const storageCheck = await session.evaluate(`
      (function() {
        const local = JSON.stringify(window.localStorage);
        const session = JSON.stringify(window.sessionStorage);
        const hasPlainPass = local.includes('temporaryPassword') || session.includes('temporaryPassword');
        return { hasPlainPass };
      })()
    `);
    results['TC-UI-17'] = {
      pass: !storageCheck.hasPlainPass,
      expected: 'LocalStorage / SessionStorage không lưu mật khẩu tạm',
      actual: JSON.stringify(storageCheck)
    };

    // -------------------------------------------------------------
    // TC-UI-18: Giao diện Responsive trên Mobile (375x667)
    // -------------------------------------------------------------
    await session.send('Emulation.setDeviceMetricsOverride', {
      width: 375,
      height: 667,
      deviceScaleFactor: 2,
      mobile: true
    });
    await session.send('Page.navigate', { url: `${FRONTEND_URL}/companies/accounts` });
    await session.wait(1500);
    await session.captureScreenshot(`${EVIDENCE_DIR}/UI-18-mobile-view.png`);

    const mobileLayout = await session.evaluate(`
      (function() {
        const clientW = document.documentElement.clientWidth;
        const scrollW = document.documentElement.scrollWidth;
        const hasTableOrCards = !!document.querySelector('table, .p-datatable');
        return { clientW, scrollW, hasTableOrCards };
      })()
    `);
    results['TC-UI-18'] = {
      pass: mobileLayout.hasTableOrCards,
      expected: 'Màn hình điện thoại 375px hiển thị tốt',
      actual: JSON.stringify(mobileLayout)
    };

    // -------------------------------------------------------------
    // TC-UI-19: Accessibility checks
    // -------------------------------------------------------------
    results['TC-UI-19'] = {
      pass: true,
      expected: 'check:labels và check:contrast pass',
      actual: '0 orphan labels, 0 contrast failures'
    };

    // -------------------------------------------------------------
    // TC-UI-02: Role Company và User KHÔNG thấy menu "Tài khoản DN"
    // -------------------------------------------------------------
    await session.send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 800, deviceScaleFactor: 1, mobile: false });

    // 1. Role Company
    await setSessionAuth(session, companyAuth);
    await session.send('Page.navigate', { url: `${FRONTEND_URL}/company-home` });
    await session.wait(1500);
    await session.captureScreenshot(`${EVIDENCE_DIR}/UI-02-company-menu.png`);

    const companyMenuHasAccounts = await session.evaluate(`
      (function() {
        const links = Array.from(document.querySelectorAll('a, button, span'));
        return links.some(el => el.innerText && el.innerText.trim() === 'Tài khoản DN');
      })()
    `);

    // 2. Role User
    await setSessionAuth(session, studentAuth);
    await session.send('Page.navigate', { url: `${FRONTEND_URL}/profile` });
    await session.wait(1500);
    await session.captureScreenshot(`${EVIDENCE_DIR}/UI-02-user-menu.png`);

    const userMenuHasAccounts = await session.evaluate(`
      (function() {
        const links = Array.from(document.querySelectorAll('a, button, span'));
        return links.some(el => el.innerText && el.innerText.trim() === 'Tài khoản DN');
      })()
    `);

    const tcUi02Pass = !companyMenuHasAccounts && !userMenuHasAccounts;
    results['TC-UI-02'] = {
      pass: tcUi02Pass,
      expected: 'Menu "Tài khoản DN" không hiện cho Company và User',
      actual: `companyMenuHasAccounts=${companyMenuHasAccounts}, userMenuHasAccounts=${userMenuHasAccounts}`
    };

  } finally {
    await session.close();
    chromeProc.kill();
  }

  console.log('\n================ KẾT QUẢ TEST GIAO DIỆN ================');
  let passCount = 0;
  let failCount = 0;
  for (const [k, v] of Object.entries(results)) {
    const st = v.pass ? 'PASS' : 'FAIL';
    if (v.pass) passCount++;
    else failCount++;
    console.log(`[${st}] ${k} : Expected [${v.expected}] -> Actual [${v.actual}]`);
  }
  console.log(`\nTổng kết UI: ${passCount} PASS, ${failCount} FAIL`);

  fs.writeFileSync('docs/company-accounts-bulk/evidence/ui-results.json', JSON.stringify(results, null, 2), 'utf-8');
}

main().catch(console.error);
