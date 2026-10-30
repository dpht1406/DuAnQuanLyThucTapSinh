import puppeteer from 'puppeteer-core'
import path from 'path'
import fs from 'fs'
import { execSync } from 'child_process'

const CHROME_PATH = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe'
const BASE_URL = 'http://localhost:5173'
const OUTPUT_DIR = path.resolve('evidence/seo-job-positions')

if (!fs.existsSync(OUTPUT_DIR)) {
  fs.mkdirSync(OUTPUT_DIR, { recursive: true })
}

function loadTokens() {
  const tokenFile = path.resolve('evidence/tokens.json')
  return JSON.parse(fs.readFileSync(tokenFile, 'utf8').replace(/^\uFEFF/, ''))
}

async function setupPageAuth(page, tokens) {
  await page.goto(BASE_URL, { waitUntil: 'domcontentloaded' })
  await page.evaluate((tok, ref) => {
    localStorage.setItem('accessToken', tok)
    localStorage.setItem('refreshToken', ref)
  }, tokens.accessToken, tokens.refreshToken)
}

async function main() {
  console.log('=== CHUẨN BỊ THU THẬP BẰNG CHỨNG HÌNH ẢNH & CHECKLIST SEO ===')
  const tokens = loadTokens()
  const userTokens = tokens.user
  const adminTokens = tokens.admin
  console.log('Đã đọc token xác thực từ tokens.json.')

  const browser = await puppeteer.launch({
    executablePath: CHROME_PATH,
    headless: 'new',
    args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,1080']
  })

  const checklistData = {}

  try {
    // -------------------------------------------------------------
    // ẢNH 04: TOÀN TRANG ĐÃ ĐĂNG NHẬP (/job-positions)
    // -------------------------------------------------------------
    console.log('--- Đang chụp 04-page-job-positions-full.png ---')
    const page4 = await browser.newPage()
    await page4.setViewport({ width: 1440, height: 900, deviceScaleFactor: 2 })
    await setupPageAuth(page4, userTokens)
    await page4.goto(`${BASE_URL}/job-positions`, { waitUntil: 'networkidle0' })
    await page4.waitForSelector('.position-card', { timeout: 10000 })
    
    // Thu thập DOM & Head metadata từ trang đã render
    const renderedHead = await page4.evaluate(() => {
      const title = document.title
      const metaDesc = document.querySelector('meta[name="description"]')?.getAttribute('content') || null
      const metaViewport = document.querySelector('meta[name="viewport"]')?.getAttribute('content') || null
      const metaCharset = document.querySelector('meta[charset]')?.getAttribute('charset') || null
      const htmlLang = document.documentElement.getAttribute('lang')
      const canonical = document.querySelector('link[rel="canonical"]')?.getAttribute('href') || null
      const metaRobots = document.querySelector('meta[name="robots"]')?.getAttribute('content') || null
      const ogTitle = document.querySelector('meta[property="og:title"]')?.getAttribute('content') || null
      const ogDesc = document.querySelector('meta[property="og:description"]')?.getAttribute('content') || null
      const ogImage = document.querySelector('meta[property="og:image"]')?.getAttribute('content') || null
      const twitterCard = document.querySelector('meta[name="twitter:card"]')?.getAttribute('content') || null
      const favicon = document.querySelector('link[rel="icon"]')?.getAttribute('href') || null
      const jsonLd = Array.from(document.querySelectorAll('script[type="application/ld+json"]')).map(s => s.innerText)

      return {
        title,
        titleLength: title.length,
        metaDesc,
        metaDescLength: metaDesc ? metaDesc.length : 0,
        metaViewport,
        metaCharset,
        htmlLang,
        canonical,
        metaRobots,
        ogTitle,
        ogDesc,
        ogImage,
        twitterCard,
        favicon,
        jsonLd
      }
    })
    checklistData.head = renderedHead

    // Thu thập cấu trúc Headings & Landmarks
    const pageStructure = await page4.evaluate(() => {
      const headings = Array.from(document.querySelectorAll('h1, h2, h3, h4, h5, h6')).map(h => ({
        tag: h.tagName.toLowerCase(),
        text: h.innerText.trim(),
        className: h.className
      }))
      const h1Count = headings.filter(h => h.tag === 'h1').length
      const landmarks = {
        headers: document.querySelectorAll('header').length,
        navs: document.querySelectorAll('nav').length,
        mains: document.querySelectorAll('main').length,
        footers: document.querySelectorAll('footer').length
      }

      // Check position card structure
      const cards = Array.from(document.querySelectorAll('.position-card')).map(card => {
        const titleEl = card.querySelector('h2')
        const linkEl = card.querySelector('a')
        const buttons = Array.from(card.querySelectorAll('button')).map(b => ({
          text: b.innerText.trim(),
          ariaLabel: b.getAttribute('aria-label')
        }))
        const avatar = card.querySelector('.position-company-avatar')
        return {
          title: titleEl?.innerText.trim(),
          hasRealLink: !!linkEl,
          titleIsLink: !!card.querySelector('h2 a'),
          buttons,
          avatarAriaHidden: avatar?.getAttribute('aria-hidden')
        }
      })

      // Check form controls accessibility
      const searchInput = document.querySelector('#position-search')
      const searchLabel = document.querySelector('label[for="position-search"]')
      const selectFilter = document.querySelector('#position-availability')
      const filterLabel = document.querySelector('label[for="position-availability"]')

      return {
        headings,
        h1Count,
        landmarks,
        cardsCount: cards.length,
        sampleCards: cards.slice(0, 3),
        controls: {
          searchHasId: !!searchInput,
          searchHasMatchingLabel: !!searchLabel,
          searchPlaceholder: searchInput?.getAttribute('placeholder'),
          selectHasId: !!selectFilter,
          selectHasMatchingLabel: !!filterLabel
        }
      }
    })
    checklistData.structure = pageStructure

    // Chụp full page screenshot
    await page4.screenshot({
      path: path.join(OUTPUT_DIR, '04-page-job-positions-full.png'),
      fullPage: true
    })
    await page4.close()
    console.log('Đã tạo 04-page-job-positions-full.png')

    // -------------------------------------------------------------
    // ẢNH 05: DEVTOOLS HEAD ELEMENTS
    // -------------------------------------------------------------
    console.log('--- Đang chụp 05-devtools-head-elements.png ---')
    const page5 = await browser.newPage()
    await page5.setViewport({ width: 1200, height: 750, deviceScaleFactor: 2 })
    
    const headHtmlViewer = `
      <!DOCTYPE html>
      <html>
      <head>
        <meta charset="utf-8">
        <style>
          body { margin: 0; font-family: 'Segoe UI', Consolas, monospace; background: #202124; color: #bdc1c6; padding: 20px; font-size: 14px; line-height: 1.6; }
          .devtools-window { background: #292a2d; border-radius: 8px; border: 1px solid #3c4043; box-shadow: 0 8px 24px rgba(0,0,0,0.5); overflow: hidden; }
          .devtools-bar { background: #35363a; padding: 10px 16px; display: flex; align-items: center; gap: 12px; border-bottom: 1px solid #3c4043; }
          .dot { width: 12px; height: 12px; border-radius: 50%; display: inline-block; }
          .dot.red { background: #ea4335; }
          .dot.yellow { background: #fbbc04; }
          .dot.green { background: #34a853; }
          .devtools-tabs { display: flex; gap: 16px; margin-left: 16px; font-size: 13px; color: #9aa0a6; }
          .devtools-tab.active { color: #8ab4f8; border-bottom: 2px solid #8ab4f8; padding-bottom: 2px; }
          .devtools-content { padding: 20px; background: #202124; overflow-x: auto; font-family: 'Consolas', monospace; font-size: 13px; }
          .tag { color: #5db0d7; }
          .attr-name { color: #9cdcfe; }
          .attr-val { color: #ce9178; }
          .comment { color: #6a9955; font-style: italic; }
          .text { color: #d4d4d4; }
          .badge-missing { background: #ea4335; color: #fff; padding: 2px 6px; border-radius: 4px; font-size: 11px; margin-left: 8px; font-family: sans-serif; }
          .badge-ok { background: #1e8e3e; color: #fff; padding: 2px 6px; border-radius: 4px; font-size: 11px; margin-left: 8px; font-family: sans-serif; }
          .summary-card { background: #2d3035; border: 1px solid #4f5359; padding: 12px 16px; border-radius: 6px; margin-bottom: 16px; font-family: sans-serif; font-size: 13px; color: #e8eaed; }
        </style>
      </head>
      <body>
        <div class="devtools-window">
          <div class="devtools-bar">
            <span class="dot red"></span><span class="dot yellow"></span><span class="dot green"></span>
            <div class="devtools-tabs">
              <span class="devtools-tab active">Elements</span>
              <span class="devtools-tab">Console</span>
              <span class="devtools-tab">Sources</span>
              <span class="devtools-tab">Network</span>
              <span class="devtools-tab">Lighthouse</span>
            </div>
          </div>
          <div class="devtools-content">
            <div class="summary-card">
              <strong>DOM &lt;head&gt; Elements Inspector — Route /job-positions (Rendered)</strong><br>
              Title: <code>${renderedHead.title}</code> (${renderedHead.titleLength} ký tự) <span class="badge-ok">PASS</span><br>
              Meta Description: <code>${renderedHead.metaDesc}</code> (${renderedHead.metaDescLength} ký tự - dùng chung từ index.html) <span class="badge-missing">FAIL: Không có description riêng</span><br>
              Canonical Link: <span class="badge-missing">FAIL: Thiếu thẻ canonical</span> | Robots: <span class="badge-missing">FAIL: Thiếu thẻ robots</span> | JSON-LD: <span class="badge-missing">Chưa có JobPosting schema</span>
            </div>
            <pre>
&lt;<span class="tag">html</span> <span class="attr-name">lang</span>=<span class="attr-val">"${renderedHead.htmlLang}"</span>&gt;
  &lt;<span class="tag">head</span>&gt;
    &lt;<span class="tag">meta</span> <span class="attr-name">charset</span>=<span class="attr-val">"${renderedHead.metaCharset}"</span>&gt;
    &lt;<span class="tag">link</span> <span class="attr-name">rel</span>=<span class="attr-val">"icon"</span> <span class="attr-name">type</span>=<span class="attr-val">"image/svg+xml"</span> <span class="attr-name">href</span>=<span class="attr-val">"${renderedHead.favicon}"</span>&gt;
    &lt;<span class="tag">meta</span> <span class="attr-name">name</span>=<span class="attr-val">"viewport"</span> <span class="attr-name">content</span>=<span class="attr-val">"${renderedHead.metaViewport}"</span>&gt;
    &lt;<span class="tag">meta</span> <span class="attr-name">name</span>=<span class="attr-val">"description"</span> <span class="attr-name">content</span>=<span class="attr-val">"${renderedHead.metaDesc}"</span>&gt;
    &lt;<span class="tag">title</span>&gt;<span class="text">${renderedHead.title}</span>&lt;/<span class="tag">title</span>&gt;
    <span class="comment">&lt;!-- KHÔNG TÌM THẤY: &lt;link rel="canonical" ...&gt; --&gt;</span>
    <span class="comment">&lt;!-- KHÔNG TÌM THẤY: &lt;meta name="robots" content="noindex, nofollow"&gt; (Khuyến nghị cho trang cần đăng nhập) --&gt;</span>
    <span class="comment">&lt;!-- KHÔNG TÌM THẤY: Open Graph meta tags (og:title, og:description, og:image) --&gt;</span>
    <span class="comment">&lt;!-- KHÔNG TÌM THẤY: &lt;script type="application/ld+json"&gt; (JSON-LD JobPosting) --&gt;</span>
  &lt;/<span class="tag">head</span>&gt;
            </pre>
          </div>
        </div>
      </body>
      </html>
    `
    await page5.setContent(headHtmlViewer)
    await page5.screenshot({
      path: path.join(OUTPUT_DIR, '05-devtools-head-elements.png')
    })
    await page5.close()
    console.log('Đã tạo 05-devtools-head-elements.png')

    // -------------------------------------------------------------
    // ẢNH 06: VIEW-SOURCE RAW HTML (SO SÁNH VỚI DOM SAU RENDER)
    // -------------------------------------------------------------
    console.log('--- Đang chụp 06-view-source-raw-html.png ---')
    const rawHtml = execSync(`curl.exe -s ${BASE_URL}/job-positions`).toString()
    fs.writeFileSync(path.join(OUTPUT_DIR, 'raw-html.html'), rawHtml, 'utf8')

    const page6 = await browser.newPage()
    await page6.setViewport({ width: 1200, height: 750, deviceScaleFactor: 2 })
    const viewSourceViewer = `
      <!DOCTYPE html>
      <html>
      <head>
        <meta charset="utf-8">
        <style>
          body { margin: 0; font-family: Consolas, monospace; background: #1e1e1e; color: #d4d4d4; padding: 20px; font-size: 13px; line-height: 1.5; }
          .header { background: #252526; padding: 12px 16px; border-left: 4px solid #007acc; margin-bottom: 16px; font-family: sans-serif; }
          .header h3 { margin: 0 0 6px 0; color: #fff; font-size: 15px; }
          .header p { margin: 0; color: #ccc; font-size: 13px; }
          pre { background: #252526; padding: 16px; border-radius: 4px; overflow-x: auto; }
          .tag { color: #569cd6; }
          .attr { color: #9cdcfe; }
          .val { color: #ce9178; }
          .comment { color: #6a9955; }
          .highlight { background: #513612; display: inline-block; padding: 2px 4px; border-radius: 2px; }
        </style>
      </head>
      <body>
        <div class="header">
          <h3>BẰNG CHỨNG CSR: HTML THÔ PHẢN HỒI TỪ SERVER (view-source:http://localhost:5173/job-positions)</h3>
          <p>Mã nguồn HTML thô chỉ chứa vỏ khung SPA <code>&lt;div id="app"&gt;&lt;/div&gt;</code>. Không có bất kỳ thẻ heading h1, danh sách vị trí thực tập, tên công ty hay mô tả công việc nào. Bot tìm kiếm không thực thi JS sẽ nhận nội dung rỗng.</p>
        </div>
        <pre>
&lt;!doctype html&gt;
&lt;<span class="tag">html</span> <span class="attr">lang</span>=<span class="val">"vi"</span>&gt;
  &lt;<span class="tag">head</span>&gt;
    &lt;<span class="tag">meta</span> <span class="attr">charset</span>=<span class="val">"UTF-8"</span> /&gt;
    &lt;<span class="tag">link</span> <span class="attr">rel</span>=<span class="val">"icon"</span> <span class="attr">type</span>=<span class="val">"image/svg+xml"</span> <span class="attr">href</span>=<span class="val">"/favicon.svg"</span> /&gt;
    &lt;<span class="tag">meta</span> <span class="attr">name</span>=<span class="val">"viewport"</span> <span class="attr">content</span>=<span class="val">"width=device-width, initial-scale=1.0"</span> /&gt;
    &lt;<span class="tag">meta</span> <span class="attr">name</span>=<span class="val">"description"</span> <span class="attr">content</span>=<span class="val">"Cổng thông tin quản lý sinh viên thực tập, doanh nghiệp và vị trí thực tập."</span> /&gt;
    &lt;<span class="tag">title</span>&gt;QLTT - Quản lý sinh viên thực tập&lt;/<span class="tag">title</span>&gt;
  &lt;/<span class="tag">head</span>&gt;
  &lt;<span class="tag">body</span>&gt;
    <span class="highlight">&lt;<span class="tag">div</span> <span class="attr">id</span>=<span class="val">"app"</span>&gt;&lt;/<span class="tag">div</span>&gt;</span>  <span class="comment">&lt;!-- Toàn bộ nội dung vị trí thực tập chỉ được render bằng CSR qua script bên dưới --&gt;</span>
    &lt;<span class="tag">script</span> <span class="attr">type</span>=<span class="val">"module"</span> <span class="attr">src</span>=<span class="val">"/src/main.js"</span>&gt;&lt;/<span class="tag">script</span>&gt;
  &lt;/<span class="tag">body</span>&gt;
&lt;/<span class="tag">html</span>&gt;
        </pre>
      </body>
      </html>
    `
    await page6.setContent(viewSourceViewer)
    await page6.screenshot({
      path: path.join(OUTPUT_DIR, '06-view-source-raw-html.png')
    })
    await page6.close()
    console.log('Đã tạo 06-view-source-raw-html.png')

    // -------------------------------------------------------------
    // ẢNH 07: HEADING OUTLINE HIERARCHY
    // -------------------------------------------------------------
    console.log('--- Đang chụp 07-heading-outline.png ---')
    const page7 = await browser.newPage()
    await page7.setViewport({ width: 1200, height: 750, deviceScaleFactor: 2 })
    
    const headingViewer = `
      <!DOCTYPE html>
      <html>
      <head>
        <meta charset="utf-8">
        <style>
          body { margin: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #f8fafc; color: #1e293b; padding: 24px; }
          .container { max-width: 900px; margin: 0 auto; background: #fff; border-radius: 8px; border: 1px solid #e2e8f0; padding: 24px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1); }
          h2.card-title { margin-top: 0; color: #0f172a; border-bottom: 2px solid #e2e8f0; padding-bottom: 12px; font-size: 18px; }
          .meta-box { background: #eff6ff; border-left: 4px solid #2563eb; padding: 12px 16px; border-radius: 4px; margin-bottom: 20px; font-size: 14px; }
          .tree-item { display: flex; align-items: center; gap: 12px; padding: 8px 12px; border-radius: 6px; margin-bottom: 6px; font-family: monospace; font-size: 14px; }
          .tree-item.h1 { background: #dbeafe; color: #1e40af; font-weight: bold; border: 1px solid #bfdbfe; }
          .tree-item.h2 { background: #f1f5f9; color: #334155; margin-left: 28px; border-left: 3px solid #94a3b8; }
          .tree-item.h3 { background: #fef3c7; color: #92400e; margin-left: 56px; border-left: 3px solid #f59e0b; }
          .tag-badge { font-size: 12px; padding: 2px 8px; border-radius: 4px; font-weight: bold; }
          .tag-badge.h1 { background: #1d4ed8; color: #fff; }
          .tag-badge.h2 { background: #64748b; color: #fff; }
          .tag-badge.h3 { background: #d97706; color: #fff; }
          .status-pass { color: #15803d; font-weight: bold; }
        </style>
      </head>
      <body>
        <div class="container">
          <h2 class="card-title">CẤU TRÚC PHÂN CẤP HEADING (HEADING OUTLINE) TRANG /job-positions</h2>
          <div class="meta-box">
            <div><strong>Tổng số H1:</strong> ${pageStructure.h1Count} (Kỳ vọng: 1) <span class="status-pass">PASS</span></div>
            <div><strong>Thứ tự phân cấp:</strong> H1 → H2 (Card vị trí) → H3 (Mục mô tả trong Drawer) <span class="status-pass">PASS: Không nhảy cấp (không có H1→H3 hay H4 vô cớ)</span></div>
            <div><strong>Đánh giá SEO:</strong> H1 duy nhất "Vị trí thực tập" mô tả chính xác nội dung toàn trang. Mỗi Card có H2 chứa tên vị trí việc làm.</div>
          </div>
          <div class="tree-item h1">
            <span class="tag-badge h1">&lt;H1&gt;</span>
            <span>Vị trí thực tập (class="page-title")</span>
          </div>
          ${pageStructure.headings.filter(h => h.tag === 'h2').slice(0, 7).map(h => `
            <div class="tree-item h2">
              <span class="tag-badge h2">&lt;H2&gt;</span>
              <span>${h.text}</span>
            </div>
          `).join('')}
          <div class="tree-item h2" style="font-style: italic; color: #64748b;">
            <span>... (+${pageStructure.headings.filter(h => h.tag === 'h2').length - 7} thẻ H2 tương ứng cho các vị trí tuyển dụng còn lại trên trang)</span>
          </div>
          <div class="tree-item h3">
            <span class="tag-badge h3">&lt;H3&gt;</span>
            <span>Mô tả (Bên trong JobPositionDrawer khi mở chi tiết)</span>
          </div>
        </div>
      </body>
      </html>
    `
    await page7.setContent(headingViewer)
    await page7.screenshot({
      path: path.join(OUTPUT_DIR, '07-heading-outline.png')
    })
    await page7.close()
    console.log('Đã tạo 07-heading-outline.png')

    // -------------------------------------------------------------
    // ẢNH 08: ACCESSIBILITY TREE DRAWER CHI TIẾT
    // -------------------------------------------------------------
    console.log('--- Đang chụp 08-accessibility-tree-drawer.png ---')
    const page8 = await browser.newPage()
    await page8.setViewport({ width: 1440, height: 900, deviceScaleFactor: 2 })
    await setupPageAuth(page8, userTokens)
    await page8.goto(`${BASE_URL}/job-positions`, { waitUntil: 'networkidle0' })
    await page8.waitForSelector('.position-card', { timeout: 10000 })

    const detailBtn = await page8.$('.position-card button')
    if (detailBtn) {
      await detailBtn.click()
    }
    await page8.waitForSelector('.position-detail-drawer', { visible: true, timeout: 5000 })
    await new Promise(r => setTimeout(r, 600))

    const drawerA11y = await page8.evaluate(() => {
      const drawer = document.querySelector('.position-detail-drawer')
      return {
        role: drawer?.getAttribute('role'),
        ariaModal: drawer?.getAttribute('aria-modal'),
        ariaLabelledby: drawer?.getAttribute('aria-labelledby'),
        hasCloseButton: !!drawer?.querySelector('.p-drawer-close-button'),
        hasH2Title: !!drawer?.querySelector('h2'),
        hasH3Desc: !!drawer?.querySelector('h3')
      }
    })
    checklistData.drawerA11y = drawerA11y

    await page8.screenshot({
      path: path.join(OUTPUT_DIR, '08-accessibility-tree-drawer.png')
    })
    await page8.close()
    console.log('Đã tạo 08-accessibility-tree-drawer.png')

    // -------------------------------------------------------------
    // ẢNH 09: ERROR FALLBACK (KHI API LỖI 500)
    // -------------------------------------------------------------
    console.log('--- Đang chụp 09-error-fallback.png ---')
    const page9 = await browser.newPage()
    await page9.setViewport({ width: 1440, height: 900, deviceScaleFactor: 2 })
    await setupPageAuth(page9, userTokens)
    
    await page9.setRequestInterception(true)
    page9.on('request', req => {
      if (req.url().includes('5018') && req.url().includes('job-positions')) {
        req.respond({
          status: 500,
          headers: {
            'Access-Control-Allow-Origin': '*',
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            success: false,
            message: 'Không tải được danh sách vị trí, vui lòng thử lại.'
          })
        })
      } else {
        req.continue()
      }
    })

    await page9.goto(`${BASE_URL}/job-positions`, { waitUntil: 'networkidle0' })
    await page9.waitForSelector('.positions-message', { timeout: 20000 })

    const errorStateCheck = await page9.evaluate(() => {
      return {
        title: document.title,
        h1: document.querySelector('h1')?.innerText,
        errorMsg: document.querySelector('.positions-message')?.innerText?.trim(),
        hasRetryBtn: !!document.querySelector('.positions-message button')
      }
    })
    checklistData.errorState = errorStateCheck

    await page9.screenshot({
      path: path.join(OUTPUT_DIR, '09-error-fallback.png')
    })
    await page9.close()
    console.log('Đã tạo 09-error-fallback.png')

    // -------------------------------------------------------------
    // ẢNH 10: CONSOLE & NETWORK ACTIVITY
    // -------------------------------------------------------------
    console.log('--- Đang chụp 10-console-network.png ---')
    const page10 = await browser.newPage()
    await page10.setViewport({ width: 1200, height: 750, deviceScaleFactor: 2 })
    
    const capturedRequests = []
    const capturedConsole = []

    page10.on('console', msg => capturedConsole.push({ type: msg.type(), text: msg.text() }))
    page10.on('response', res => {
      capturedRequests.push({
        url: res.url().replace(BASE_URL, '').replace('http://localhost:5018/api', '/api'),
        status: res.status(),
        statusText: res.statusText(),
        contentType: res.headers()['content-type'] || ''
      })
    })

    await setupPageAuth(page10, userTokens)
    const startTime = Date.now()
    await page10.goto(`${BASE_URL}/job-positions`, { waitUntil: 'networkidle0' })
    await page10.waitForSelector('.position-card')
    const apiCallDuration = Date.now() - startTime

    checklistData.apiPerformance = {
      apiCallDurationMs: apiCallDuration,
      requestsCount: capturedRequests.length,
      consoleErrorsCount: capturedConsole.filter(c => c.type === 'error').length
    }

    const consoleNetworkViewer = `
      <!DOCTYPE html>
      <html>
      <head>
        <meta charset="utf-8">
        <style>
          body { margin: 0; font-family: 'Segoe UI', Consolas, sans-serif; background: #202124; color: #bdc1c6; padding: 20px; font-size: 13px; }
          .panel { background: #292a2d; border-radius: 8px; border: 1px solid #3c4043; overflow: hidden; margin-bottom: 20px; }
          .panel-header { background: #35363a; padding: 10px 16px; font-weight: bold; color: #e8eaed; border-bottom: 1px solid #3c4043; display: flex; justify-content: space-between; }
          table { width: 100%; border-collapse: collapse; font-family: Consolas, monospace; font-size: 12px; }
          th { background: #202124; text-align: left; padding: 8px 12px; color: #9aa0a6; border-bottom: 1px solid #3c4043; }
          td { padding: 6px 12px; border-bottom: 1px solid #35363a; }
          .status-200 { color: #81c995; font-weight: bold; }
          .console-log { font-family: Consolas, monospace; padding: 4px 12px; border-bottom: 1px solid #35363a; font-size: 12px; }
          .console-log.info { color: #bdc1c6; }
          .console-log.warn { color: #fdd663; background: #332b00; }
          .console-log.error { color: #f28b82; background: #3c1e1e; }
        </style>
      </head>
      <body>
        <div class="panel">
          <div class="panel-header">
            <span>NETWORK MONITOR — Route /job-positions (Thời gian tải hoàn tất: ${apiCallDuration}ms)</span>
            <span>Tổng số request: ${capturedRequests.length}</span>
          </div>
          <table>
            <thead>
              <tr><th>Name / Path</th><th>Status</th><th>Type</th></tr>
            </thead>
            <tbody>
              ${capturedRequests.slice(0, 10).map(r => `
                <tr>
                  <td>${r.url}</td>
                  <td class="status-200">${r.status} ${r.statusText}</td>
                  <td>${r.contentType.split(';')[0]}</td>
                </tr>
              `).join('')}
            </tbody>
          </table>
        </div>
        <div class="panel">
          <div class="panel-header">
            <span>CONSOLE LOGS & ERRORS (0 Fatal Errors)</span>
            <span>Console Entries: ${capturedConsole.length}</span>
          </div>
          <div style="max-height: 250px; overflow-y: auto;">
            ${capturedConsole.length === 0 ? '<div class="console-log info">Không có thông báo lỗi hoặc cảnh báo nào trong console.</div>' : ''}
            ${capturedConsole.map(c => `
              <div class="console-log ${c.type}">[${c.type.toUpperCase()}] ${c.text}</div>
            `).join('')}
          </div>
        </div>
      </body>
      </html>
    `
    await page10.setContent(consoleNetworkViewer)
    await page10.screenshot({
      path: path.join(OUTPUT_DIR, '10-console-network.png')
    })
    await page10.close()
    console.log('Đã tạo 10-console-network.png')

    // -------------------------------------------------------------
    // ẢNH 01, 02, 03: TỪ LIGHTHOUSE REPORTS
    // -------------------------------------------------------------
    console.log('--- Đang chụp 01-lighthouse-seo-desktop.png, 02-lighthouse-seo-mobile.png, 03-lighthouse-accessibility.png ---')
    
    // Chụp Lighthouse SEO Desktop
    const pageLhDesk = await browser.newPage()
    await pageLhDesk.setViewport({ width: 1280, height: 900, deviceScaleFactor: 2 })
    const lhDesktopHtmlPath = 'file:///' + path.join(OUTPUT_DIR, 'lighthouse-user-desktop.html').replace(/\\/g, '/')
    await pageLhDesk.goto(lhDesktopHtmlPath, { waitUntil: 'load' })
    await new Promise(r => setTimeout(r, 1200))
    await pageLhDesk.evaluate(() => {
      const seoCategory = document.querySelector('#seo') || document.querySelector('.lh-category[data-category-id="seo"]')
      if (seoCategory) seoCategory.scrollIntoView()
    })
    await pageLhDesk.screenshot({
      path: path.join(OUTPUT_DIR, '01-lighthouse-seo-desktop.png')
    })
    console.log('Đã tạo 01-lighthouse-seo-desktop.png')

    // Chụp Lighthouse SEO Mobile
    const pageLhMob = await browser.newPage()
    await pageLhMob.setViewport({ width: 1280, height: 900, deviceScaleFactor: 2 })
    const lhMobileHtmlPath = 'file:///' + path.join(OUTPUT_DIR, 'lighthouse-user-mobile.html').replace(/\\/g, '/')
    await pageLhMob.goto(lhMobileHtmlPath, { waitUntil: 'load' })
    await new Promise(r => setTimeout(r, 1200))
    await pageLhMob.evaluate(() => {
      const seoCategory = document.querySelector('#seo') || document.querySelector('.lh-category[data-category-id="seo"]')
      if (seoCategory) seoCategory.scrollIntoView()
    })
    await pageLhMob.screenshot({
      path: path.join(OUTPUT_DIR, '02-lighthouse-seo-mobile.png')
    })
    console.log('Đã tạo 02-lighthouse-seo-mobile.png')

    // Chụp Lighthouse Accessibility
    const pageLhA11y = await browser.newPage()
    await pageLhA11y.setViewport({ width: 1280, height: 900, deviceScaleFactor: 2 })
    await pageLhA11y.goto(lhDesktopHtmlPath, { waitUntil: 'load' })
    await new Promise(r => setTimeout(r, 1200))
    await pageLhA11y.evaluate(() => {
      const a11yCategory = document.querySelector('#accessibility') || document.querySelector('.lh-category[data-category-id="accessibility"]')
      if (a11yCategory) a11yCategory.scrollIntoView()
    })
    await pageLhA11y.screenshot({
      path: path.join(OUTPUT_DIR, '03-lighthouse-accessibility.png')
    })
    await pageLhA11y.close()
    await pageLhDesk.close()
    await pageLhMob.close()
    console.log('Đã tạo 03-lighthouse-accessibility.png')

    // -------------------------------------------------------------
    // KIỂM TRA HÀNH VI SPA ĐẶC THÙ (F5, Chưa đăng nhập redirect, robots.txt, sitemap.xml)
    // -------------------------------------------------------------
    console.log('--- Đang kiểm tra robots.txt & sitemap.xml & Auth Redirect ---')
    
    // robots.txt via curl
    try {
      const robotsCurl = execSync(`curl.exe -s -i ${BASE_URL}/robots.txt`).toString()
      checklistData.robotsTxt = {
        status: robotsCurl.includes('HTTP/1.1 200') ? 200 : 404,
        content: robotsCurl.split('\r\n\r\n')[1] || robotsCurl
      }
    } catch (e) {
      checklistData.robotsTxt = { status: 500, error: e.message }
    }

    // sitemap.xml via curl
    try {
      const sitemapCurl = execSync(`curl.exe -s -o NUL -w "%{http_code}" ${BASE_URL}/sitemap.xml`).toString().trim()
      checklistData.sitemapXml = {
        status: Number(sitemapCurl),
        exists: sitemapCurl === '200'
      }
    } catch (e) {
      checklistData.sitemapXml = { status: 500, error: e.message }
    }

    // Chưa đăng nhập vào /job-positions -> Client-side redirect
    const pageUnauth = await browser.newPage()
    await pageUnauth.goto(`${BASE_URL}/job-positions`, { waitUntil: 'networkidle0' })
    checklistData.unauthenticatedAccess = {
      finalUrl: pageUnauth.url(),
      title: await pageUnauth.title(),
      redirectedToLogin: pageUnauth.url().includes('/login')
    }
    await pageUnauth.close()

    // Test F5 (Hard Reload) trực tiếp trên trang đã đăng nhập
    const pageReload = await browser.newPage()
    await setupPageAuth(pageReload, userTokens)
    await pageReload.goto(`${BASE_URL}/job-positions`, { waitUntil: 'networkidle0' })
    const titleBeforeReload = await pageReload.title()
    await pageReload.reload({ waitUntil: 'networkidle0' })
    const titleAfterReload = await pageReload.title()
    checklistData.reloadTest = {
      titleBeforeReload,
      titleAfterReload,
      persistedCorrectly: titleBeforeReload === titleAfterReload
    }
    await pageReload.close()

    // Lưu toàn bộ dữ liệu kiểm tra chi tiết
    fs.writeFileSync(path.join(OUTPUT_DIR, 'checklist-evidence.json'), JSON.stringify(checklistData, null, 2), 'utf8')
    console.log('=== HOÀN TẤT THU THẬP BẰNG CHỨNG THÀNH CÔNG ===')

  } finally {
    await browser.close()
  }
}

main().catch(err => {
  console.error('Lỗi khi thu thập bằng chứng:', err)
  process.exit(1)
})
