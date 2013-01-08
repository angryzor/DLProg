require("json")

local onlydownloadbest = true

local function split(str, delim)
	return function (s, tail)
		local i = tail:find(delim)
		if i then
			return tail:sub(i+1), tail:sub(1,i-1)
		else
			return nil, tail
		end
	end, nil, str
end

local function downloadVideo()
	local vidname = util:html_unescape(page.HTML:match('<meta name="title" content="([^"]*)">'))
	local flashvars = util:html_unescape(page.HTML:match('<embed type="application/x%-shockwave%-flash"[^>]*flashvars="([^"]*)"'))
	local fvmap = {}
	for tail, head in split(flashvars,"&") do
		for val, key in split(head, "=") do
			fvmap[key] = util:url_unescape(val)
		end
	end
	
	local fsmap = {}
	for tail, head in split(fvmap.url_encoded_fmt_stream_map,",") do
		fsmap[#fsmap+1] = {}
		for tail2, head2 in split(head, "&") do
			for val, key in split(head2, "=") do
				fsmap[#fsmap][key] = util:url_unescape(val)
			end
		end
	end

--[[	
	log("Found urls:\n")
	for i, v in pairs(fsmap) do
		log("Type: " .. v.type .. "\n")
		log("Quality: " .. v.quality .. "\n")
		log("URL: " .. v.url .. "\n")
		log("-------------------------------------------------------\n")
	end
--]]
	
	for i, v in pairs(fsmap) do
		if v.type:match('video/mp4') then
			queue:add_download_link(v.url, "", vidname .. "(" .. v.quality .. ")" .. ".mp4")
			if onlydownloadbest then break end
		elseif v.type:match('video/x%-flv') then
			if not onlydownloadbest then
				queue:add_download_link(v.url, "", vidname .. "(" .. v.quality .. ")" .. ".flv")
			end
		elseif v.type:match('video/webm') then
			if not onlydownloadbest then
				queue:add_download_link(v.url, "", vidname .. "(" .. v.quality .. ")" .. ".webm")
			end
		else
			log("unknown video type: " .. v.type)
		end
	end
end

local function queueAllUserVideos()
	-- user page. download all videos
	local user = page.HTML:match('window%.uid = "([^"]+)";')
	log(user)
	local totalpages = tonumber(page.HTML:match("playnav%.updateScrollbox%(&#39;playnav%-play%-uploads%-scrollbox&#39;, ([0-9]+)%)"))
	log(totalpages)
	--local session_token = page.HTML:match("window%.ajax_session_info = 'session_token=([^']+)';")
	log(session_token)
	
	for i = 0, totalpages - 1 do
		local messages = '[{"type":"box_method","request":{"name":"user_playlist_navigator","x_position":1,"y_position":-2,"palette":"default","method":"load_playlist_page","params":{"playlist_name":"uploads","encrypted_playlist_id":"uploads","query":"","encrypted_shmoovie_id":"uploads","page_num":' .. i .. ',"view":"play","playlist_sort":"date","playlist_sort_direction":"desc"}}}]'
		local body = 'session_token=' --[[.. util:url_escape(session_token)]] .. '&messages=' .. util:url_escape(messages)
		queue:add_page_link_ex("http://www.youtube.com/profile_ajax?action_ajax=1&uid=" .. user .. "&new=1&box_method=load_playlist_page&box_name=user_playlist_navigator",1,"POST","application/x-www-form-urlencoded; charset=UTF-8",body)
	end
end

local function parseVideoPage()
	local body = page.HTML:sub(10)
	local d = json.decode(body)
	log(d[1].data)
	log("*************")
	local h = d[1].data
	for vLnk in h:gmatch('<a href="([^"]+)" class="ux%-thumb%-wrap') do
		queue:add_page_link(vLnk,nil)
	end
end
		
if not page.UserData then
	if page.URL:match("/watch") then
		downloadVideo()
	elseif page.URL:match("http://www%.youtube%.com/user/[%w_-]+$") or page.URL:match("http://www%.youtube%.com/[%w_-]+$") then
		queueAllUserVideos()
	else
		log("Wrong url.")
	end
elseif page.UserData == 1 then
	parseVideoPage()
else
	log("Strange error.")
end
