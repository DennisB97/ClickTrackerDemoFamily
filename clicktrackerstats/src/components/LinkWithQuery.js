// Copyright 2021 Dennis Baeckstroem 
import {Link, useLocation} from 'react-router-dom';

// used to Link together with additional params
export const LinkWithQuery = ({children, to, ...props}) =>
{
const {search} = useLocation();

return(
<Link to={to + search} {...props}>
{children}
</Link>
);
};