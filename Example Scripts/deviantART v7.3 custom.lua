-- Add a link for a DiFi query
function addDiFiQuery(query,offset)
	queue:add_page_link("http://www.deviantart.com/global/difi/?c[]=Resources;htmlFromQuery;" .. query .. "," .. offset .. ",24,thumb150,&t=php",1)
end

-- Gets the value of a query var in the current URL
function getQVar(name)
	local ret
	
	for v in string.gmatch(page.URL,"[%?#&]" .. name .. "=([^&]+)") do
		ret = util:url_unescape(v)
	end
	
	return ret
end

function badUrl()
	log("Malformed starting URL!\n\nURL must have one of these forms:\n\nhttp://areallycooldeviant.deviantart.com/gallery\nhttp://areallycooldeviant.deviantart.com/favourites\nhttp://www.deviantart.com/?order=9&q=my+search+terms\nhttp://browse.deviantart.com/?order=9&q=my+search+terms\nhttp://www.deviantart.com/#order=9&q=my+search+terms\nhttp://browse.deviantart.com/#order=9&q=my+search+terms\nhttp://browse.deviantart.com/#order=9&catpath=photography/conceptual&q=my+search+terms\nhttp://browse.deviantart.com/#catpath=photography\nhttp://www.deviantart.com/#catpath=photography")
end

function grpCollEnum(q,i)
	return string.match(page.HTML,'<link rel="alternate" type="application/rss%+xml"[^>]*href="[^"]+' .. string.gsub(string.gsub(util:url_escape(q),"%%","%%%%"),"%-","%%-") .. '%%2F(%w+)()',i)
end

function addCollection(q)
	local coll
	
	coll = string.match(page.URL,"[%w_%-]+%.deviantart%.com/%w+/(%d+)")
	if coll then
		addDiFiQuery(q .. "/" .. coll,0)
	else
		-- Check if group page, cuz then we need subcolls
		if string.match(page.HTML, "<title>#.-</title>") then
			local i
			coll,i = grpCollEnum(q,0)
			while coll do
				addDiFiQuery(q .. "/" .. coll,0)
				coll,i = grpCollEnum(q,i)
			end
		end
		addDiFiQuery(q,0)
	end
end

function addBrowser()
	local q = getQVar("q")
	local catpath = string.match(page.URL,"[%w_%-]+%.deviantart%.com/([%w_%-/]+)/[#%?]?")
	local qry
	
	if catpath then
		qry = "in:" .. catpath
	else
		qry = "meta:all"
	end
	
	qry = qry .. "+boost:popular"
	
	if q then
		qry = qry .. "+" .. q
	end
	
	addDiFiQuery(qry,0)	
end

function writeOut(fn,html)
	local f = io.openfordl(fn,"w")
	if f then
		f:write([[<html>
<body>
]])
		f:write(html)
		f:write([[
</body>
</html>
]])
		f:close()
	end
end

function queueNextFriendsPage()
	local url = page.HTML:match('<a [^>]*href="([^"]+)">Next</a>')
	if url then
		queue:add_page_link(url,nil)
	end
end

function writeFriends(user,mode)
	local f = io.open(user .. "/friends.txt",mode)
	for fr in page.HTML:gmatch("<a class='u'[^>]*>(.-)</a>") do
		f:write(fr .. "\n")
	end
	f:close()
end

function addFriends(user)
	if page.URL:match("offset=0") then
		writeFriends(user,"w")
	else
		writeFriends(user,"a")
	end
	queueNextFriendsPage()
end

-- L0: A Root URL
if page.UserData == nil then
	log("processing root\r\n")
	local domain,firstsubfolder=string.match(page.URL,"([%w_%-]+)%.deviantart%.com/(%w*)")
	
	if (domain and firstsubfolder) then 
	
		-- Favourites collection?
		if firstsubfolder == "favourites" then
			addCollection("favby:" .. domain)
			
		-- Gallery?
		elseif firstsubfolder == "gallery" then
			addCollection("gallery:" .. domain)
		
		elseif firstsubfolder == "friends" then
			addFriends(domain)
			
		-- Browsing/Search results?
		elseif (domain == "www" or domain == "browse") then
			addBrowser()
			
		else
			badUrl()
		end
	else
		badUrl()
	end;
			
-- L1: A DiFi Query
elseif page.UserData == 1 then
	log("processing a DiFi Query" .. page.URL .. "\r\n")
	for url in string.gmatch(page.HTML,'<!%-%- %^TTT %-%-><a href="([^"]+)"') do
		queue:add_page_link(url,2)
	end
	if string.match(page.HTML,'s:4:"more";b:1') then
		local query,offset = string.match(util:url_unescape(page.URL),"Resources;htmlFromQuery;([^,]+),(%d+),24,thumb150")
		
		addDiFiQuery(query,offset + 24)
	end
	
-- L2: An art page
elseif page.UserData == 2 then
	local dlurl = string.match(page.HTML,'href="(http://www%.deviantart%.com/download/[^"]+)"')
	local desc,title,author = string.match(page.HTML,'(<div[^>]*id="artist%-comments"[^>]*>.-<h1[^>]*>.-<a[^>]*>(.-)</a>.-<small>.-<a[^>]*>(.-)</a>.-<i usr class="gr1 gb gb1"></i>.-</div>.-</div>)')
	if not author then
		title,author = string.match(page.HTML,'<div id="deviation">.-<h1><a[^>]*>(.-)</a>.-<small>.-<a class="u"[^>]*>([^<]+)')
	end
	if dlurl then
		local t = string.match(dlurl,'[^/]+$')
		if not t then t = title end
		queue:add_download_link(dlurl,author,t)
		writeOut("__dA_descriptions/" .. author .. "/" .. t .. ".desc.html",desc)
	else
		dlurl = string.match(page.HTML,'src="([^">]+)"[^>]*class="fullview')
		if not dlurl then -- maybe it's a flash vid
			dlurl = string.match(page.HTML,'<iframe[^>]*class="flashtime"[^>]*src="([^"]+)"')
		end
		if not dlurl then -- maybe it's a journal post
			local t = page.HTML:match('<div class="journal%-wrapper2">(.-)</div></div><script type="text/javascript">')
			if t then
				writeOut(author .. "/" .. title .. ".html",t)
				return
			end
		end
		if not dlurl then
			log(page.HTML)
			error("Unexpected media type.")
		end
		--local realurl = string.gsub(dlurl,"\\/","/")
		local t = title .. "_by_" .. author .. "." .. string.match(dlurl,'[^%.]+$')
		queue:add_download_link(dlurl,author,t) --string.match(realurl,'/([^/]+)$'))
		writeOut("__dA_descriptions/" .. author .. "/" .. t .. ".desc.html",desc)
	end

end
