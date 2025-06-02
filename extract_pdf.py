import pdfplumber
import re
import csv
from pathlib import Path
import logging
from datetime import datetime

# --- CONFIGURATION --------------------------------------------------

logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')

PDF_FILES = ["Kcet-2020.pdf", "Kcet-2021.pdf", "Kcet-2022.pdf", "Kcet-2023.pdf"]
YEAR_RE   = re.compile(r'(\d{4})')

# Your cleaned categories
KNOWN_CATEGORIES = [
    'GM', 'GMK', 'GMR', 'SCG', 'SCK', 'SCR', 'STG', 'STK', 'STR',
    '1G', '1K', '1R', '2AG', '2AK', '2AR', '2BG', '2BK', '2BR',
    '3AG', '3AK', '3AR', '3BG', '3BK', '3BR'
]

# Pattern to pick up the start of each college block (e.g. "5  E005  R. V. College...")
COLLEGE_HEADER = re.compile(r'^\s*\d+\s+E\d{3}\b')

# --- UTILITIES -----------------------------------------------------

def extract_year_from_filename(fname):
    m = YEAR_RE.search(fname)
    return m.group(1) if m else None

def clean_rank(r):
    return ''.join(filter(str.isdigit, r)) or None

def clean_category(cat):
    cat = cat.strip().upper()
    return cat if cat in KNOWN_CATEGORIES else None

# --- CORE LOGIC ----------------------------------------------------

def process_pdf(path: Path):
    year = extract_year_from_filename(path.name)
    if not year:
        logging.warning(f"Could not detect year in {path.name}, skipping.")
        return []

    logging.info(f"→ Opening {path.name} for year {year}")
    records = []
    with pdfplumber.open(str(path)) as pdf:
        current_college = None
        buffer = []   # lines under the current college

        # collect all lines across all pages
        for page in pdf.pages:
            text = page.extract_text() or ""
            for raw in text.splitlines():
                line = raw.strip()
                if not line:
                    continue

                # New college start?
                if COLLEGE_HEADER.match(line):
                    # flush previous
                    if current_college and buffer:
                        records += flush_college_block(year, current_college, buffer)
                    current_college = line
                    buffer = []
                else:
                    buffer.append(line)

        # final flush
        if current_college and buffer:
            records += flush_college_block(year, current_college, buffer)

    return records

def flush_college_block(year, header_line, lines):
    """
    Given a college header and its subsequent lines, 
    1) detect the category header inside lines,
    2) merge wrapped rows,
    3) emit (year, college, branch, category, rank).
    """
    out = []
    college_name = header_line
    cat_header_idx = None

    # 1) find the index of the line that lists the categories
    for idx, line in enumerate(lines):
        parts = line.split()
        cats = [clean_category(p) for p in parts if clean_category(p)]
        if len(cats) >= len(parts)/2 and len(cats) >= 5:
            category_list = cats
            cat_header_idx = idx
            break

    if cat_header_idx is None:
        logging.warning(f"  No category header in block for {college_name}")
        return out

    # lines with actual data start after the category header
    data_lines = lines[cat_header_idx+1:]

    # 2) merge wrapped lines: accumulate until we see at least one digit
    merged = []
    buf = ""
    for line in data_lines:
        if re.search(r'\d', line):
            if buf:
                buf += " " + line
                merged.append(buf.strip())
                buf = ""
            else:
                merged.append(line)
        else:
            # continuation of previous
            buf = (buf + " " + line).strip()

    # 3) split each merged line into branch + ranks
    for entry in merged:
        parts = entry.split()
        # find first numeric index
        for i,p in enumerate(parts):
            if p.isdigit():
                break
        else:
            continue  # no rank found

        branch = " ".join(parts[:i])
        ranks  = parts[i:]
        if len(ranks) != len(category_list):
            logging.warning(f"  Mismatch in {college_name}: found {len(ranks)} ranks vs {len(category_list)} categories")
            continue

        for cat, rank in zip(category_list, ranks):
            cr = clean_rank(rank)
            if cr:
                out.append([year, college_name, branch, cat, cr])

    return out

# --- MAIN ---------------------------------------------------------

def main():
    all_records = []
    for fname in PDF_FILES:
        p = Path(fname)
        if p.exists():
            all_records += process_pdf(p)
        else:
            logging.error(f"File not found: {fname}")

    # write CSV
    with open("kcet_cleaned.csv", "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["Year","CollegeName","Branch","Category","ClosingRank"])
        w.writerows(all_records)

    logging.info(f"✔ Wrote {len(all_records)} rows to kcet_cleaned.csv")

if __name__ == "__main__":
    logging.info(f"Script started: {datetime.now()}")
    main()
    logging.info(f"Script ended:   {datetime.now()}")
