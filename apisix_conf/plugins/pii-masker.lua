local core = require("apisix.core")
local ngx = ngx
local string = string

local schema = {
    type = "object",
    properties = {
        mask_phone = {type = "boolean", default = true},
        mask_id = {type = "boolean", default = true}
    }
}

local plugin_name = "pii-masker"

local _M = {
    version = 0.1,
    priority = 1000,
    name = plugin_name,
    schema = schema,
}

function _M.check_schema(conf)
    return core.schema.check(schema, conf)
end

function _M.body_filter(conf, ctx)
    local body = core.response.hold_body_chunk(ctx)
    if not body then
        return
    end

    -- 簡單的 Regex 替換
    if conf.mask_phone then
        body = ngx.re.gsub(body, "09[0-9]{8}", "[PHONE_MASKED]")
    end
    
    if conf.mask_id then
        body = ngx.re.gsub(body, "[A-Z][12][0-9]{8}", "[ID_MASKED]")
    end

    ngx.arg[1] = body
end

return _M
